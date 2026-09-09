---
rule: CII-DT-021
severity: error
packs: [cen-en16931 1.3.16, xrechnung 3.0.2]
business_terms: []
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# CII-DT-021

## EN16931-CII-validation.xslt

> Name should not be present

```xpath
not(ram:Name) or (self::ram:AdditionalReferencedDocument and ram:TypeCode='916')
```

Shipped in cen-en16931 1.3.16, xrechnung 3.0.2.

Fired on 1 file(s) of the verification corpus, for example `mustang/ZUGFeRD_Extended__Abschlagsrechnung_SubInvoiceLine_u_LV_Nr_.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
