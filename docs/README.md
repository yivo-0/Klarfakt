# Documentation

Published at [klarfakt.dev](https://klarfakt.dev).

- [Pricing](pricing.md) — the revenue threshold, what a commercial licence covers,
  and the rule pack update commitment.

## Rule pages

`_rules/` holds one page per validation rule, scaffolded by `tools/rule-pages.py` from the
artefacts themselves: the publisher's own message, the failing XPath for each syntax, the packs
that carry the rule, and how often it fired across the verification corpus. Everything below the
marker in a page is written by hand and the script never touches it, so it can be re-run after a
pack bump.

The 57 currently there are the rules the corpus actually reports, out of 1,782 defined. Nothing is
published: Jekyll ignores an underscore directory that is not declared as a collection, and every
page also carries `published: false` until its prose is written.

## Articles

Written as posts under `_posts/`, so the dated filename is the publication date.

- [Four hundred bytes](_posts/2026-09-06-four-hundred-bytes.md) — a 424-byte PDF
  that terminates a .NET process, why a `catch` cannot help, and the compression
  bomb next to it.
- [Validating European e-invoices in .NET, without a JVM](_posts/2026-09-06-en16931-without-java.md) —
  why `XslCompiledTransform` cannot run the EN 16931 artefacts, what changed in
  May 2026, and four implementation details that are not written down anywhere
  obvious.
