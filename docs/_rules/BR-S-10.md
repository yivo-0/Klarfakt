---
rule: BR-S-10
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: [BG-23, BT-118, BT-120, BT-121]
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# BR-S-10

## EN16931-CII-validation.xslt

> A VAT Breakdown (BG-23) with VAT Category code (BT-118) "Standard rate" shall not have a VAT exemption reason code (BT-121) or VAT exemption reason text (BT-120).

```xpath
not(../ram:ExemptionReason) and not (../ram:ExemptionReasonCode)
```

## EN16931-UBL-validation.xslt

> A VAT breakdown (BG-23) with VAT Category code (BT-118) "Standard rate" shall not have a VAT exemption reason code (BT-121) or VAT exemption reason text (BT-120).

```xpath
not(cbc:TaxExemptionReason) and not(cbc:TaxExemptionReasonCode)
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
