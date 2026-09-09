"""Maintainer tool: scaffolds one page per validation rule from the rule packs themselves.

An access point rejects an invoice with a rule id and no guidance, so the first thing anyone does
is search for the id. What they find is the specification restating the rule in the words that
already failed to help. These pages are the answer to that search, and the half of each one that
can be derived is derived rather than retyped: the publisher's own message, the failing XPath, the
artefact it came from, and how often it fired across the corpus.

Everything below the marker in a page is written by hand and is never touched again, so this can
be re-run after a pack bump to refresh the mechanical half without losing the prose.

    python tools/rule-pages.py                 # the rules the corpus actually reports
    python tools/rule-pages.py --all           # every rule in every shipped artefact
    python tools/rule-pages.py --list          # print the rules and their corpus counts, write nothing

Needs the rule packs restored: klarfakt rules restore
"""

import argparse
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MANIFEST = os.path.join(ROOT, "src", "Klarfakt", "RulePacks.json")
PAGES = os.path.join(ROOT, "docs", "_rules")

# The packs that carry business rules. The schemas pack is XSD and the visualisation pack renders.
RULE_PACKS = ("cen-en16931", "peppol-bis", "xrechnung")

MARKER = "<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->"

# Both artefact families emit SVRL, and they disagree about where the id lives: KoSIT writes it as
# an attribute on the element, CEN writes it as a nested xsl:attribute. Neither is wrong and both
# have to be read.
ASSERT = re.compile(
    r"<svrl:(?P<kind>failed-assert|successful-report)\b(?P<head>[^>]*)>(?P<body>.*?)</svrl:(?P=kind)>",
    re.DOTALL)
XSL_ATTRIBUTE = re.compile(
    r'<xsl:attribute\s+name="(?P<name>id|test|flag)"\s*>(?P<value>.*?)</xsl:attribute>', re.DOTALL)
HEAD_ATTRIBUTE = re.compile(r'(?P<name>id|test|flag)="(?P<value>[^"]*)"')
SVRL_TEXT = re.compile(r"<svrl:text\b[^>]*>(?P<value>.*?)</svrl:text>", re.DOTALL)
TAG = re.compile(r"<[^>]*>")
BUSINESS_TERM = re.compile(r"\b(?:BT|BG)-\d+(?:-\d+)?\b")
RULE_ID = re.compile(r"^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+$")

# corpus/validation-report-<tfm>.md, written by the test suite.
REPORT_COUNT = re.compile(r"^- (?P<rule>[A-Z][A-Za-z0-9-]*): (?P<count>\d+)$")
REPORT_FILE = re.compile(r"^- `(?P<file>[^`]+)` \((?P<ruleset>[^)]+)\): (?P<rules>.+)$")


def unescape(value):
    for entity, character in (("&lt;", "<"), ("&gt;", ">"), ("&quot;", '"'),
                              ("&apos;", "'"), ("&#34;", '"'), ("&amp;", "&")):
        value = value.replace(entity, character)
    return value


def flatten(value):
    """SVRL text carries markup of its own — xsl:value-of, mostly. The sentence is what matters."""
    return " ".join(unescape(TAG.sub("", value)).split())


def packs():
    with open(MANIFEST, encoding="utf-8") as handle:
        manifest = json.load(handle)["packs"]

    return [pack for pack in manifest if pack["id"] in RULE_PACKS]


def artefacts(pack):
    directory = os.path.join(ROOT, "rules", pack["id"], pack["version"])
    for name in sorted(pack["files"]):
        if name.lower().endswith((".xsl", ".xslt")):
            yield name, os.path.join(directory, *name.split("/"))


def occurrences():
    """Every assertion in every shipped validation artefact, keyed by rule id."""
    found = {}
    missing = []

    for pack in packs():
        for name, path in artefacts(pack):
            if not os.path.exists(path):
                missing.append(path)
                continue

            with open(path, encoding="utf-8") as handle:
                source = handle.read()

            for match in ASSERT.finditer(source):
                fields = {k: v for k, v in HEAD_ATTRIBUTE.findall(match.group("head"))}
                fields.update({m.group("name"): flatten(m.group("value"))
                               for m in XSL_ATTRIBUTE.finditer(match.group("body"))})

                rule = fields.get("id", "").strip()
                if not RULE_ID.match(rule):
                    continue

                text = SVRL_TEXT.search(match.group("body"))
                message = flatten(text.group("value")) if text else ""
                message = re.sub(rf"^\[{re.escape(rule)}\]\s*-?\s*", "", message)

                found.setdefault(rule, []).append({
                    "pack": pack["id"],
                    "version": pack["version"],
                    "artefact": name,
                    "flag": fields.get("flag", "").strip() or "fatal",
                    "test": unescape(fields.get("test", "").strip()),
                    "message": message,
                    "reported": match.group("kind") == "successful-report",
                })

    if missing:
        sys.exit(f"{len(missing)} artefacts are not on disk, starting with {missing[0]}.\n"
                 "Run 'klarfakt rules restore' first.")

    return found


def corpus():
    """How often each rule fired, and a file that triggers it, from the suite's own report."""
    counts, examples = {}, {}

    for name in sorted(os.listdir(os.path.join(ROOT, "corpus"))):
        if not name.startswith("validation-report"):
            continue

        with open(os.path.join(ROOT, "corpus", name), encoding="utf-8") as handle:
            for line in handle:
                line = line.rstrip("\n")

                count = REPORT_COUNT.match(line)
                if count:
                    counts[count.group("rule")] = max(
                        counts.get(count.group("rule"), 0), int(count.group("count")))
                    continue

                entry = REPORT_FILE.match(line)
                if entry:
                    for rule in (r.strip() for r in entry.group("rules").split(",")):
                        examples.setdefault(rule, entry.group("file"))

    return counts, examples


def page(rule, entries, count, example, existing):
    """The mechanical half. `existing` is whatever a person has already written underneath."""
    syntaxes = sorted({e["artefact"] for e in entries})
    terms = sorted(
        {t for e in entries for t in BUSINESS_TERM.findall(e["message"])},
        key=lambda t: (t.split("-")[0], int(t.split("-")[1]), t))
    severity = "warning" if all(e["flag"] not in ("fatal", "error") for e in entries) else "error"

    # Deduplicated: the same rule appears in KoSIT's copy of the CEN artefact and in CEN's own, and
    # a page repeating one expression four times says less than a page showing the two that differ.
    variants, seen = [], set()
    for entry in sorted(entries, key=lambda e: (e["pack"], e["artefact"])):
        key = (entry["test"], entry["message"])
        if key in seen:
            continue
        seen.add(key)
        variants.append(entry)

    where = sorted({f"{e['pack']} {e['version']}" for e in entries})

    lines = [
        "---",
        f"rule: {rule}",
        f"severity: {severity}",
        f"packs: [{', '.join(where)}]",
        f"business_terms: [{', '.join(terms)}]" if terms else "business_terms: []",
        f"corpus_hits: {count}" if count else "corpus_hits: 0",
        "# Flip to true once the sections below are written.",
        "published: false",
        "---",
        "",
        f"# {rule}",
        "",
    ]

    for entry in variants:
        lines.append(f"## {entry['artefact']}")
        lines.append("")
        lines.append(f"> {entry['message']}" if entry["message"] else "> (the artefact carries no message)")
        lines.append("")
        if entry["test"]:
            lines.append("```xpath")
            lines.append(entry["test"])
            lines.append("```")
            lines.append("")

    lines.append(f"Shipped in {', '.join(where)}.")
    if count:
        lines.append("")
        lines.append(f"Fired on {count} file(s) of the verification corpus"
                     + (f", for example `{example}`." if example else "."))
    lines.append("")
    lines.append(MARKER)

    written_half = existing.lstrip("\n") if existing is not None else "\n".join([
        "## What it means",
        "",
        "TODO",
        "",
        "## Why it fires",
        "",
        "TODO",
        "",
        "## How to fix it",
        "",
        "TODO",
        "",
    ])

    return "\n".join(lines).rstrip("\n") + "\n\n" + written_half


def written(path):
    """Whatever a person has put below the marker, so re-running cannot destroy it."""
    if not os.path.exists(path):
        return None

    with open(path, encoding="utf-8") as handle:
        content = handle.read()

    index = content.find(MARKER)
    return content[index + len(MARKER):] if index >= 0 else None


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--all", action="store_true",
                        help="every rule in the artefacts, not only the ones the corpus reports")
    parser.add_argument("--list", action="store_true", help="print what would be written")
    arguments = parser.parse_args()

    found = occurrences()
    counts, examples = corpus()

    if arguments.all:
        rules = sorted(found)
    else:
        rules = sorted(rule for rule in counts if rule in found)
        # XSD is Klarfakt's own id for the schema parser's codes, not a rule anyone can look up.
        unknown = sorted(rule for rule in counts if rule not in found and rule != "XSD")
        if unknown:
            print(f"not in any shipped artefact, skipped: {', '.join(unknown)}")

    if arguments.list:
        for rule in sorted(rules, key=lambda r: (-counts.get(r, 0), r)):
            print(f"  {rule:<26} {counts.get(rule, 0):>3} corpus hit(s)  "
                  f"{len(found[rule])} occurrence(s)")
        print(f"\n{len(rules)} rules, {len(found)} defined across the packs")
        return 0

    os.makedirs(PAGES, exist_ok=True)
    fresh = kept = 0

    for rule in rules:
        path = os.path.join(PAGES, f"{rule}.md")
        existing = written(path)
        kept += existing is not None
        fresh += existing is None

        # newline="\n" and utf-8 without a BOM, to match every other file in docs/.
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(page(rule, found[rule], counts.get(rule, 0), examples.get(rule), existing))

    print(f"{len(rules)} pages in {os.path.relpath(PAGES, ROOT)}: "
          f"{fresh} new, {kept} refreshed with their prose kept")
    return 0


if __name__ == "__main__":
    sys.exit(main())
