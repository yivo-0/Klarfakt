---
rule: PEPPOL-COMMON-R049
severity: error
packs: [peppol-bis 3.0.21]
business_terms: []
corpus_hits: 28
# Flip to true once the sections below are written.
published: false
---

# PEPPOL-COMMON-R049

## PEPPOL-EN16931-UBL.xslt

> Swedish organization number MUST be stated in the correct format.

```xpath
string-length(normalize-space()) = 10 and string(number(normalize-space())) != 'NaN' and u:checkSEOrgnr(normalize-space())
```

Shipped in peppol-bis 3.0.21.

Fired on 28 file(s) of the verification corpus, for example `cen-en16931/issue116.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
