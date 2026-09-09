---
rule: CII-DT-031
severity: error
packs: [cen-en16931 1.3.16, xrechnung 3.0.2]
business_terms: []
corpus_hits: 4
# Flip to true once the sections below are written.
published: false
---

# CII-DT-031

## EN16931-CII-validation.xslt

> currencyID should not be present

```xpath
not(@currencyID)
```

Shipped in cen-en16931 1.3.16, xrechnung 3.0.2.

Fired on 4 file(s) of the verification corpus, for example `mustang/facturFrMinimum.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
