---
rule: CII-DT-027
severity: error
packs: [cen-en16931 1.3.16, xrechnung 3.0.2]
business_terms: []
corpus_hits: 2
# Flip to true once the sections below are written.
published: false
---

# CII-DT-027

## EN16931-CII-validation.xslt

> FormattedIssueDateTime should not be present

```xpath
not(ram:FormattedIssueDateTime) or self::ram:InvoiceReferencedDocument
```

Shipped in cen-en16931 1.3.16, xrechnung 3.0.2.

Fired on 2 file(s) of the verification corpus, for example `mustang/factur-x-extended.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
