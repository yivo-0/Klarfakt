"""Reports when an upstream publisher has released a newer version of a pinned rule pack.

Detection is automated; the bump is not. A rule-pack update can change the verdict on invoices
that previously passed, so it needs a human to read the upstream changelog and the corpus diff.
This script tells you an update exists and what changed; `tools/fetch-rules.py` performs the bump.

    python tools/check-rule-updates.py            # print a report, exit 1 if anything is behind
    python tools/check-rule-updates.py --issue     # also open or update a tracking GitHub issue
"""

import json
import os
import subprocess
import sys

MANIFEST = os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "..", "src", "Klarfakt", "RulePacks.json")

ISSUE_TITLE = "Rule packs behind upstream"
UNKNOWN_TITLE = "Rule pack check could not reach upstream"
ISSUE_LABEL = "rule-packs"


def open_or_comment(title, report):
    existing = gh("issue", "list", "--state", "open", "--search", f'"{title}" in:title',
                  "--json", "number", "--jq", ".[0].number")

    if existing:
        gh("issue", "comment", existing, "--body", report)
        print(f"\nCommented on issue #{existing}")
    else:
        gh("issue", "create", "--title", title, "--body", report, "--label", ISSUE_LABEL)
        print(f"\nOpened issue: {title}")


def gh(*args):
    """stdout, or None when the call failed. Use gh_checked where a failure matters."""
    result = subprocess.run(["gh", *args], capture_output=True, text=True)
    return result.stdout.strip() if result.returncode == 0 else None


def gh_checked(*args):
    """
    stdout and no error, or None and why.

    The reason is kept because the two failures need different answers. A repository that has
    stopped publishing releases is upstream news; a token without scope, a rate limit or a moved
    repository is this check being broken. Collapsing both into None is how the whole thing came
    to report success when it had learned nothing -- every pack fell into 'unknown', 'unknown' was
    printed and never acted on, and the run exited 0.
    """
    result = subprocess.run(["gh", *args], capture_output=True, text=True)
    if result.returncode == 0:
        return result.stdout.strip(), None

    detail = " ".join(result.stderr.split())[:200] or f"gh exited {result.returncode}"
    return None, detail


def latest_release(repo):
    raw, error = gh_checked(
        "api", f"repos/{repo}/releases/latest", "--jq", "{tag: .tag_name, date: .published_at, url: .html_url}")

    if error is not None:
        return None, error

    try:
        return json.loads(raw), None
    except json.JSONDecodeError:
        return None, f"could not read the release for {repo}: {raw[:120]!r}"


def main():
    with open(MANIFEST, encoding="utf-8") as handle:
        packs = json.load(handle)["packs"]

    # Several packs can share one upstream repository; report each pin.
    behind, current, unknown = [], [], []

    for pack in packs:
        latest, error = latest_release(pack["repo"])
        if error is not None:
            unknown.append((pack, error))
            continue
        if latest["tag"] != pack["tag"]:
            behind.append((pack, latest))
        else:
            current.append(pack)

    for pack in current:
        print(f"  current   {pack['id']:<14} {pack['tag']}")
    for pack, error in unknown:
        print(f"  UNKNOWN   {pack['id']:<14} {pack['repo']}: {error}")
    for pack, latest in behind:
        print(f"  BEHIND    {pack['id']:<14} pinned {pack['tag']}  ->  {latest['tag']} ({latest['date'][:10]})")

    # A pack this could not read is not a pack that is up to date, and saying so was the whole
    # defect: it printed the total pack count in a sentence claiming they were all current.
    if unknown:
        print(f"\n{len(unknown)} of {len(packs)} packs could not be checked. This says nothing about "
              "whether they are current, and the 30-day update commitment rests on this check.")

        if "--issue" in sys.argv:
            report = "\n".join([
                "The rule pack check could not reach the upstream release for:",
                "",
                *[f"- `{pack['id']}` (`{pack['repo']}`): {error}" for pack, error in unknown],
                "",
                "Until this is fixed the weekly check cannot tell whether a pack is behind.",
            ])
            open_or_comment(UNKNOWN_TITLE, report)

        return 1

    if not behind:
        print(f"\nAll {len(current)} packs are on the latest upstream release.")
        return 0

    body = [
        "The following rule packs are behind their upstream release.",
        "",
        "| Pack | Pinned | Latest | Published | Release |",
        "|---|---|---|---|---|",
    ]
    for pack, latest in behind:
        body.append(
            f"| `{pack['id']}` | `{pack['tag']}` | `{latest['tag']}` | {latest['date'][:10]} | {latest['url']} |")
    body += [
        "",
        "To bump: edit the version and asset names in `tools/fetch-rules.py`, run it, commit the",
        "regenerated `src/Klarfakt/RulePacks.json`, and review the corpus reports for changed",
        "verdicts before releasing. A rule update can invalidate invoices that previously passed.",
    ]
    report = "\n".join(body)

    print()
    print(report)

    if "--issue" in sys.argv:
        open_or_comment(ISSUE_TITLE, report)

    return 1


if __name__ == "__main__":
    sys.exit(main())
