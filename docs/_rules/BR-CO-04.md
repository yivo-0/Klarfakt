---
rule: BR-CO-04
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: [BG-25, BT-151]
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# BR-CO-04

## EN16931-CII-validation.xslt

> Each Invoice line (BG-25) shall be categorized with an Invoiced item VAT category code (BT-151).

```xpath
(ram:SpecifiedLineTradeSettlement/ram:ApplicableTradeTax[upper-case(ram:TypeCode) = 'VAT']/ram:CategoryCode)
```

## EN16931-UBL-validation.xslt

> Each Invoice line (BG-25) shall be categorized with an Invoiced item VAT category code (BT-151).

```xpath
(cac:Item/cac:ClassifiedTaxCategory[cac:TaxScheme/(normalize-space(upper-case(cbc:ID))='VAT')]/cbc:ID)
```

Shipped in cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 1 file(s) of the verification corpus, for example `mustang/UC11_F202600022_EXTENDED_FX_CII_BT-X-589Only_on_GROUP_Line.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
