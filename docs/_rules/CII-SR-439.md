---
rule: CII-SR-439
severity: error
packs: [cen-en16931 1.3.16, xrechnung 3.0.2]
business_terms: []
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# CII-SR-439

## EN16931-CII-validation.xslt

> ChargeAmount should exist maximum once

```xpath
count(ram:NetPriceProductTradePrice/ram:ChargeAmount) = 1
```

Shipped in cen-en16931 1.3.16, xrechnung 3.0.2.

Fired on 1 file(s) of the verification corpus, for example `mustang/UC11_F202600022_EXTENDED_FX_CII_BT-X-589Only_on_GROUP_Line.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
