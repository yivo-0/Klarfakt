# Changelog

Notable changes, newest first. This project follows [Semantic Versioning](https://semver.org).

Rule pack versions are called out separately from library versions, because a
pack bump can change the verdict on an invoice that previously passed while the
library itself is unchanged.

## Unreleased

### Fixed

- **A rule-pack problem still ended a folder run.** Preview.2 stopped a Peppol
  CII invoice throwing out of `Validate`, but any `RulePackException` raised
  inside the folder loop — an altered or missing artefact — still left
  `Parallel.ForEach` as an `AggregateException` that no handler caught: exit 127
  and a stack trace over two files, where the same problem over one file gave
  exit 2 and a sentence. The batch now rethrows the worker's own exception, so
  every file count reaches the same handler.
- `--lang fr` reached the KoSIT stylesheet untouched, where the value names a
  decimal-format that exists only for `de` and `en`, and produced nine lines of
  Saxon internals and exit 2 for a typo. It is a usage error, exit 64, like
  `--rules` and `--parallel`.
- The weekly rule-pack check could report every pack current when its release
  lookup had failed. A pack it could not read is now reported as unknown and the
  run fails, because the 30-day update commitment rests on that check.
- `NOTICE` named AngleSharp 1.7.3 while the package pins 1.8.0.

### Changed

- `validate --json` emits camelCase keys (`ruleId`, `findings`), matching
  `info --json`. Anything reading the old PascalCase keys needs updating;
  `tools/reference-diff.py` has been.
- `InvoiceRenderer.ToHtml` throws `ArgumentException` for a language the
  visualisation ships no labels for, rather than failing inside Saxon.

## 1.0.0-preview.2

### Fixed

- **A Peppol CII invoice ended a whole batch run.** Peppol BIS Billing CII
  Invoice is a registered Peppol document type, so it is ordinary inbound traffic
  for anyone registered to receive CII — but the pinned KoSIT BIS configuration
  ships UBL artefacts only. Choosing that rule set from the document and then
  asking for artefacts that do not exist threw out of `InvoiceValidator.Validate`,
  and `Parallel.ForEach` re-threw it as an `AggregateException` that no handler
  caught: a stack trace and exit 127, which is not in the documented exit codes,
  with every other invoice in the folder unreported. A rule set chosen from the
  document now falls back to EN 16931, the way any other profile without rules of
  its own already did. A rule set the caller names explicitly is still refused
  rather than quietly swapped.
- `RulePackCatalog.Covers` took no syntax, so that same document reported
  `ProfileCovered = true` while nothing here could judge it. Coverage now requires
  artefacts for the document's syntax as well as its identifier.
- The refusal message claimed "Peppol BIS Billing 3.0 is defined for UBL only".
  It is not — OpenPEPPOL publishes CII Schematron. What is UBL-only is the pinned
  configuration Klarfakt runs, and the message says so.
- `--parallel abc` and `--rules nonsense` exited 2, "a file could not be
  processed", for what is a mistake in the command line. Both are usage errors,
  and both exit 64.
- The `HttpResponseMessage` in `RestoreAsync` was never disposed.

## 1.0.0-preview.1

First package. Reads, validates and renders EN 16931 invoices with no JVM, no
Node process and no network call outside `rules restore`.

### Rule packs

| Pack | Version | Source |
|---|---|---|
| `schemas` | 2026-08-31 | UBL 2.1 + CII D16B as redistributed by KoSIT |
| `cen-en16931` | 1.3.16 | ConnectingEurope/eInvoicing-EN16931 |
| `peppol-bis` | 3.0.21 | itplr-kosit/validator-configuration-bis |
| `xrechnung` | 3.0.2 | itplr-kosit/validator-configuration-xrechnung |
| `visualization` | 3.0.2 | itplr-kosit/xrechnung-visualization |

### Added

- `InvoiceDocument` reads UBL 2.1 Invoice and CreditNote, CII D16B, and the XML
  embedded in a hybrid Factur-X or ZUGFeRD 2.x PDF.
- One canonical `EInvoice` model across both syntaxes. A malformed date or amount
  becomes a finding rather than an exception.
- `InvoiceValidator` runs XML Schema, then the EN 16931 rules, then the national
  CIUS, in the order the German reference validator uses. Findings carry the rule
  id, severity, SVRL flag, location, failing XPath test, business terms, the
  offending value where the rule pointed at one, and the artefact that produced
  them.
- `InvoiceRenderer` produces a self-contained HTML page from KoSIT's official
  XRechnung visualisation, in German or English.
- `RulePackCatalog` restores artefacts from pinned upstream releases and verifies
  each against the manifest SHA-256 before first use.
- `DocumentLimits` bounds input size, attachment size and PDF name-tree traversal.
- `klarfakt` command line: `rules restore` / `rules verify`, `validate`,
  `render`, `info`. Folder input, CSV and JSON output, `--strict`, `--parallel`,
  and Ctrl+C reporting what finished.
- `ValidationResult.SchemaChecked` and `ProfileCovered`, so a verdict says what it
  actually covered.
- Two long-form articles and a pricing page, published at
  [klarfakt.dev](https://klarfakt.dev) with an Atom feed.
- `SECURITY.md`, `CONTRIBUTING.md`, a code of conduct, issue templates and a
  package icon.

### Hardening

Found before this first package shipped, so no released version ever carried
them. Written down because they say what the limits are actually worth.

- An attachment's declared encoding decided whether the size limit applied.
  Anything with `/DecodeParms` went to a path that decompressed the whole
  attachment before the limit was consulted, and `/Predictor 1` means "no
  prediction" — the same bytes. A 64 MB payload against a 1 KB limit allocated
  134 MB. Plain Flate is bounded whatever framing declares it, and a filter
  Klarfakt does not decode is refused rather than expanded.
- `/Filter [/FlateDecode]` is valid PDF and means `/Filter /FlateDecode`, but was
  read as a name and threw, rejecting a readable invoice as an unreadable file.
- `RulePackCatalog.Covers` read a version out of the middle of the specification
  identifier, so a national CIUS such as NLCIUS, and anything appended to a
  supported profile, reported as fully covered while being judged against the
  base rules alone. It matches whole identifiers now.
- Two embedded files under one name were settled by the order the collectors ran
  in, so a hybrid PDF could be judged on a copy that conformant readers ignore.
  Identical duplicates are accepted; two different files under one name are not.
- `DocumentLimits.MaxTotalAttachmentBytes` bounds a whole document, and bounds it
  during inflation rather than after. A per-attachment cap bounded nothing on its
  own.
- The CSV export quoted separators but not a leading `=`, `+`, `-` or `@`, so an
  invoice's own `cbc:CustomizationID` could become a formula in a spreadsheet.
- `RestoreAsync` made one attempt per release asset, so a single 504 from GitHub
  ended the restore. Four attempts, backing off 1s, 2s and 4s; a 404 still fails
  at once, because a pinned tag that is gone will not appear.
- The command line kept every option it did not read. A mistyped `--stict` went
  into the flag set and the run reported exit 0 for an archive nothing had
  validated strictly; `klarfakt info ./invoices --csv report.csv` parsed cleanly,
  wrote no file and said nothing, because `info` never looks at `--csv`. Each
  command accepts only what it reads now, and anything else exits 64.
- `tools/reference-diff.py` compared both sides by file name, and basenames repeat
  across the corpora. Eight of the 143 files were matched against a different
  file's verdict, and the reference validator's own reports overwrote each other
  on disk before the comparison started. The agreement held once corrected; the
  count did not, and **97 of 97** is the number this release reports.
- Cancelling a batch reported the exit code of the files that finished. It exits
  130 and says how many were never checked.

### Verification

- All 309 CEN rule-test files replayed, every `<success>` and `<error>`
  expectation, proving agreement per rule rather than in aggregate.
- 97 of 97 comparable files agree rule for rule with KoSIT's validationtool
  v1.6.3 on every CI run.
- 989 tests on net8.0 and net10.0.
- Four defects found in published upstream examples, documented in the test suite.
