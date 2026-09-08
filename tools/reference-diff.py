"""Compares Klarfakt's verdicts against the German reference validator, rule by rule.

Klarfakt runs the publishers' own artefacts, so it should agree with the tool the publishers
ship. This is what turns that into a claim anyone can check: it runs KoSIT's validationtool over
a corpus, runs Klarfakt over the same files, and diffs the rule ids each reports.

Java is needed to run the *reference*. It is not needed to use Klarfakt, which is the point.

    python tools/reference-diff.py                 # writes corpus/reference-diff.md
    python tools/reference-diff.py --corpus corpus/kosit-xrechnung

Exits non-zero when the two disagree about a file in a way that is not recorded in EXPECTED below.
"""

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import urllib.request
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The reference implementation of the validation framework, pinned. The scenario configuration is
# NOT pinned here: it is read from the manifest, so the comparison always runs the same
# configuration release Klarfakt validates against. Comparing against a different one would
# produce disagreements that say nothing about either tool.
VALIDATOR_REPO = "itplr-kosit/validator"
VALIDATOR_TAG = "v1.6.3"
VALIDATOR_JAR = "validator-1.6.3-standalone.jar"

CACHE = os.path.join(ROOT, ".tmp", "reference")

# Files the two are known to disagree about, and why. A disagreement not listed here fails the run.
# Keyed by file name, valued by a reason that has to be written by a person who checked it.
EXPECTED: dict[str, str] = {}

# A floor on how much has to be comparable for a run to mean anything, set from the corpora this
# defaults to. Without it, anything that stopped Klarfakt reading files reported perfect agreement
# about nothing at all and exited zero. Raise it when the corpora grow.
MINIMUM_COMPARABLE = 90

MESSAGE = re.compile(
    r'<rep:message\b[^>]*?level="(?P<level>[^"]*)"[^>]*?code="(?P<code>[^"]*)"', re.DOTALL)
MESSAGE_REVERSED = re.compile(
    r'<rep:message\b[^>]*?code="(?P<code>[^"]*)"[^>]*?level="(?P<level>[^"]*)"', re.DOTALL)
REFERENCE = re.compile(r"<rep:documentReference>(.*?)</rep:documentReference>", re.DOTALL)


def download(url, destination):
    if os.path.exists(destination):
        return destination

    os.makedirs(os.path.dirname(destination), exist_ok=True)
    print(f"  downloading {url}")
    # Write alongside and rename: a transfer cut halfway would otherwise leave a truncated file
    # that the check above treats as a finished download, and CI caches this directory.
    partial = destination + ".part"
    with urllib.request.urlopen(url) as response, open(partial, "wb") as handle:
        shutil.copyfileobj(response, handle)
    os.replace(partial, destination)

    return destination


def configuration():
    """The scenario configuration, taken from the same release the xrechnung rule pack pins."""
    with open(os.path.join(ROOT, "src", "Klarfakt", "RulePacks.json"), encoding="utf-8") as handle:
        packs = json.load(handle)["packs"]

    pack = next(p for p in packs if p["id"] == "xrechnung")
    asset = next(iter(pack["files"].values()))["origin"].split("!", 1)[0]
    directory = os.path.join(CACHE, f"config-{pack['version']}")

    if not os.path.isdir(directory):
        archive = download(
            f"https://github.com/{pack['repo']}/releases/download/{pack['tag']}/{asset}",
            os.path.join(CACHE, asset))
        with zipfile.ZipFile(archive) as zipped:
            zipped.extractall(directory)

    return directory, f"{pack['repo']}@{pack['tag']}"


def java():
    for candidate in (os.environ.get("JAVA_HOME"), None):
        binary = os.path.join(candidate, "bin", "java") if candidate else shutil.which("java")
        if binary and (shutil.which(binary) or os.path.exists(binary) or os.path.exists(binary + ".exe")):
            return check_version(binary)

    sys.exit("No java on PATH and JAVA_HOME is not set. The reference validator needs Java 11+.")


def check_version(binary):
    """
    Refuses a Java too old to load the validator. Java 8 fails inside the class loader and writes
    no reports, which is indistinguishable from "the reference matched no scenario" unless someone
    looks — a green-looking run that compared nothing at all.
    """
    reported = subprocess.run([binary, "-version"], capture_output=True, text=True).stderr
    version = re.search(r'version "(\d+)(?:\.(\d+))?', reported)
    if version is None:
        return binary

    major = int(version.group(1))
    if major == 1:
        major = int(version.group(2) or 0)

    if major < 11:
        sys.exit(f"{binary} is Java {major}; the reference validator needs 11+. "
                 "Set JAVA_HOME to a newer JDK.")

    return binary


def key(path):
    """
    A file's path relative to the repository root, which is what identifies it.

    Basenames repeat across the corpora — BR-01.xml is both a CreditNote and an Invoice unit test,
    and 04.01a-INVOICE_ubl.xml is an XRechnung extension case in one corpus and a plain Mustang
    invoice in another. Keying by one compared eight of the 143 files against a different file's
    verdict, and reported the result as agreement.
    """
    path = path.replace("\\", "/")
    absolute = path if os.path.isabs(path) else os.path.join(ROOT, path)
    return os.path.relpath(absolute, ROOT).replace("\\", "/")


def reference_verdicts(files, reports, config):
    """Runs KoSIT's validationtool and reads the rule ids it reported per file."""
    jar = download(
        f"https://github.com/{VALIDATOR_REPO}/releases/download/{VALIDATOR_TAG}/{VALIDATOR_JAR}",
        os.path.join(CACHE, VALIDATOR_JAR))

    # Cleared per run: reports are named after the input file, so leftovers from another corpus
    # would silently be read as this one's.
    shutil.rmtree(reports, ignore_errors=True)

    # One run per source directory. The validator names each report after its input file and writes
    # them all into one folder, so two inputs sharing a basename overwrote each other on disk and
    # the reference side simply lost a verdict. Within a directory the filesystem already
    # guarantees names are unique, so grouping this way cannot collide.
    groups = {}
    for path in files:
        groups.setdefault(os.path.dirname(path), []).append(path)

    verdicts = {}
    for index, directory in enumerate(sorted(groups)):
        output = os.path.join(reports, str(index))
        os.makedirs(output, exist_ok=True)
        verdicts.update(run_reference(jar, config, output, directory, groups[directory]))

    # A group that produced nothing is not an error — corpus/facturx is ZUGFeRD 1.x, which matches
    # no scenario the reference ships, and the comparison already records those as not comparable.
    # Producing nothing anywhere is the failure worth stopping for.
    if not verdicts:
        sys.exit("the reference validator judged none of the corpus")

    return verdicts


def run_reference(jar, config, output, directory, files):
    command = [java(), "-jar", jar,
               "-s", os.path.join(config, "scenarios.xml"),
               "-r", config,
               "-o", output] + files

    # input="" rather than DEVNULL: the tool calls System.in.available() to detect piped input, and
    # on Windows that throws on a null device handle but is happy with an empty pipe.
    result = subprocess.run(command, input="", capture_output=True, text=True)

    # The exit code is the number of documents it rejected, which is a verdict and not an error.
    if not os.listdir(output):
        if "Exception" in result.stderr or "Error" in result.stderr:
            print(result.stderr[-2000:])
            sys.exit(f"the reference validator failed on {key(directory)} (exit {result.returncode})")

        print(f"  {key(directory)}: the reference matched no scenario for these {len(files)} files")
        return {}

    verdicts = {}
    for name in os.listdir(output):
        if not name.endswith(".xml"):
            continue

        with open(os.path.join(output, name), encoding="utf-8") as handle:
            report = handle.read()

        source = REFERENCE.search(report)
        if source is None:
            continue

        # Rebuilt from the directory this run was given rather than parsed out of the report: the
        # basename is unambiguous within one directory, which is the whole reason for the grouping.
        path = os.path.join(directory, os.path.basename(source.group(1).replace("\\", "/")))

        # No scenario matched means the reference declined to judge the file at all, which is not
        # the same as judging it clean. Left out so it is skipped rather than read as agreement.
        if "rep:scenarioMatched" not in report:
            continue

        verdicts[key(path)] = {
            normalise(match.group("code"))
            for pattern in (MESSAGE, MESSAGE_REVERSED)
            for match in pattern.finditer(report)
            if match.group("level") == "error"
        }

    return verdicts


def normalise(code):
    """The reference reports the schema parser's own code; Klarfakt reports one id for all of them."""
    return "XSD" if code.startswith(("cvc-", "sch-")) else code


def klarfakt_verdicts(corpora):
    command = os.environ.get("KLARFAKT_CLI")
    command = command.split() if command else [
        "dotnet", "run", "--project", os.path.join(ROOT, "src", "Klarfakt.Cli"),
        "-c", "Release", "--no-build", "--"]

    result = subprocess.run(command + ["validate", *corpora, "-r", "--json"],
                            capture_output=True, text=True, cwd=ROOT)
    if not result.stdout.strip():
        print(result.stderr[-2000:])
        sys.exit("klarfakt produced no output")

    return {
        key(report["File"]): (
            report["Status"],
            {finding["RuleId"] for finding in report["Findings"] if finding["Severity"] == "Error"},
        )
        for report in json.loads(result.stdout)
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--corpus", nargs="+", default=[
        os.path.join("corpus", "kosit-xrechnung"),
        os.path.join("corpus", "mustang"),
        os.path.join("corpus", "facturx"),
    ])
    parser.add_argument("--out", default=os.path.join("corpus", "reference-diff.md"))
    arguments = parser.parse_args()

    files = []
    for relative in arguments.corpus:
        corpus = os.path.join(ROOT, relative)
        if not os.path.isdir(corpus):
            sys.exit(f"{corpus} is missing. Run tools/fetch-corpus.py first.")

        files.extend(
            os.path.join(base, name)
            for base, _, names in os.walk(corpus)
            for name in names
            if name.lower().endswith(".xml"))

    files.sort()
    config, configuration_ref = configuration()
    print(f"comparing {len(files)} files against {VALIDATOR_REPO}@{VALIDATOR_TAG}")

    reference = reference_verdicts(files, os.path.join(CACHE, "reports"), config)
    ours = klarfakt_verdicts(arguments.corpus)

    rows, disagreements, skipped = [], [], []

    for path in files:
        name = key(path)
        theirs = reference.get(name)
        mine = ours.get(name)

        # Not every file is a fair comparison: ZUGFeRD 1.x is out of scope here and the reference
        # matches no scenario for some inputs. Those are recorded, not counted as disagreement.
        if theirs is None:
            skipped.append((name, "the reference matched no scenario"))
            continue

        # The reference judged this document. Klarfakt failing to read it is a disagreement about
        # the document, not a reason to drop it from the count — anything that broke every read
        # used to report "0/0 agree" and exit 0.
        if mine is None or mine[0] == "error":
            disagreements.append((name, theirs, {"(Klarfakt could not process this file)"}))
            rows.append((name, theirs, theirs, set()))
            continue

        missed, extra = theirs - mine[1], mine[1] - theirs
        if missed or extra:
            disagreements.append((name, missed, extra))

        rows.append((name, theirs, missed, extra))

    unexpected = [d for d in disagreements if d[0] not in EXPECTED]
    write_report(arguments.out, configuration_ref, rows, disagreements, skipped)

    print(f"\n{len(rows) - len(disagreements)}/{len(rows)} comparable files agree rule for rule "
          f"with the reference validator ({len(skipped)} not comparable)")

    # A run that compared nothing is not a run that agreed about everything.
    if len(rows) < MINIMUM_COMPARABLE:
        print(f"\nonly {len(rows)} files were comparable, expected at least {MINIMUM_COMPARABLE}. "
              "Nothing meaningful was checked.")
        return 1

    if unexpected:
        for name, missed, extra in unexpected:
            print(f"  {name}:"
                  + (f" reference reported {sorted(missed)}" if missed else "")
                  + (f" we reported {sorted(extra)}" if extra else ""))
        return 1

    return 0


def write_report(out, configuration_ref, rows, disagreements, skipped):
    path = os.path.join(ROOT, out)
    os.makedirs(os.path.dirname(path), exist_ok=True)

    with open(path, "w", encoding="utf-8") as handle:
        handle.write("# Agreement with the reference validator\n\n")
        handle.write(
            f"Klarfakt against KoSIT's validationtool `{VALIDATOR_TAG}`, both using the scenario "
            f"configuration from `{configuration_ref}` — the same release the `xrechnung` rule pack "
            "pins, so the two run identical artefacts.\n\n")
        handle.write(f"**{len(rows) - len(disagreements)} of {len(rows)} comparable files agree "
                     f"rule for rule.**\n\n")

        if disagreements:
            handle.write("## Disagreements\n\n")
            handle.write("| File | Reference reported | Klarfakt reported | Known |\n|---|---|---|---|\n")
            for name, missed, extra in disagreements:
                handle.write(f"| `{name}` | {', '.join(sorted(missed)) or '—'} | "
                             f"{', '.join(sorted(extra)) or '—'} | "
                             f"{EXPECTED.get(name, '**no**')} |\n")
            handle.write("\n")

        if skipped:
            handle.write(f"## Not comparable ({len(skipped)})\n\n")
            handle.write("| File | Reason |\n|---|---|\n")
            for name, reason in skipped:
                handle.write(f"| `{name}` | {reason} |\n")
            handle.write("\n")

        with_findings = [(name, theirs) for name, theirs, _, _ in rows if theirs]
        handle.write(f"## Files both reject, and why ({len(with_findings)})\n\n")
        handle.write("| File | Rules |\n|---|---|\n")
        for name, theirs in with_findings:
            handle.write(f"| `{name}` | {', '.join(sorted(theirs))} |\n")

    print(f"wrote {os.path.normpath(path)}")


if __name__ == "__main__":
    sys.exit(main())
