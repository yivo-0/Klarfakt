---
rule: UBL-SR-37
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: []
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# UBL-SR-37

## EN16931-UBL-validation.xslt

> Item price discount shall occur maximum once

```xpath
(count(cac:Price/cac:AllowanceCharge/cbc:Amount) <= 1)
```

Shipped in cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 1 file(s) of the verification corpus, for example `mustang/testout-ZF2PushItemChargesAllowances.ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
