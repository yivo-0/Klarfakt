---
rule: BR-CL-17
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: []
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# BR-CL-17

## EN16931-CII-validation.xslt

> Invoice tax categories MUST be coded using UNCL 5305 code list

```xpath
((not(contains(normalize-space(.), ' ')) and contains(' AE L M E S Z G O K B ', concat(' ', normalize-space(.), ' '))))
```

## EN16931-UBL-validation.xslt

> Invoice tax categories MUST be coded using UNCL5305 code list

```xpath
( ( not(contains(normalize-space(.),' ')) and contains( ' AE L M E S Z G O K B ',concat(' ',normalize-space(.),' ') ) ) )
```

Shipped in cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 1 file(s) of the verification corpus, for example `mustang/UBL-CreditNote-2.1-Example.ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
