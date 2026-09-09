---
rule: PEPPOL-EN16931-R002
severity: error
packs: [peppol-bis 3.0.21]
business_terms: []
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# PEPPOL-EN16931-R002

## PEPPOL-EN16931-UBL.xslt

> No more than one note is allowed on document level, unless both the buyer and seller are German organizations.

```xpath
count(cbc:Note) <= 1 or ($supplierCountryIsDE and $customerCountryIsDE)
```

Shipped in peppol-bis 3.0.21.

Fired on 1 file(s) of the verification corpus, for example `mustang/BT-128.ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
