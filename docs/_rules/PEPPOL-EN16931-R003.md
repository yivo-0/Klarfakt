---
rule: PEPPOL-EN16931-R003
severity: error
packs: [peppol-bis 3.0.21]
business_terms: []
corpus_hits: 2
# Flip to true once the sections below are written.
published: false
---

# PEPPOL-EN16931-R003

## PEPPOL-EN16931-UBL.xslt

> A buyer reference or purchase order reference MUST be provided.

```xpath
cbc:BuyerReference or cac:OrderReference/cbc:ID
```

Shipped in peppol-bis 3.0.21.

Fired on 2 file(s) of the verification corpus, for example `mustang/testout-ZF2PushAllowances.ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
