---
rule: PEPPOL-COMMON-R040
severity: error
packs: [peppol-bis 3.0.21]
business_terms: []
corpus_hits: 6
# Flip to true once the sections below are written.
published: false
---

# PEPPOL-COMMON-R040

## PEPPOL-EN16931-UBL.xslt

> GLN must have a valid format according to GS1 rules.

```xpath
matches(normalize-space(), '^[0-9]+$') and u:gln(normalize-space())
```

Shipped in peppol-bis 3.0.21.

Fired on 6 file(s) of the verification corpus, for example `cen-rule-tests/BIS_Billing_30-Elhandel.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
