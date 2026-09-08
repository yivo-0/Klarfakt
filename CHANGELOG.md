# Changelog

Notable changes, newest first. This project follows [Semantic Versioning](https://semver.org).

Rule pack versions are called out separately from library versions, because a
pack bump can change the verdict on an invoice that previously passed while the
library itself is unchanged.

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
- Cancelling a batch reported the exit code of the files that finished. It exits
  130 and says how many were never checked.

### Verification

- All 309 CEN rule-test files replayed, every `<success>` and `<error>`
  expectation, proving agreement per rule rather than in aggregate.
- 100 of 100 comparable files agree rule for rule with KoSIT's validationtool
  v1.6.3 on every CI run.
- 989 tests on net8.0 and net10.0.
- Four defects found in published upstream examples, documented in the test suite.
