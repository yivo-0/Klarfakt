---
rule: BR-S-05
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: [BG-25, BT-151, BT-152]
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# BR-S-05

## EN16931-CII-validation.xslt

> In an Invoice line (BG-25) where the Invoiced item VAT category code (BT-151) is "Standard rated" the Invoiced item VAT rate (BT-152) shall be greater than zero.

## EN16931-CII-validation.xsl

> In an Invoice line (BG-25) where the Invoiced item VAT category code (BT-151) is "Standard rated" the Invoiced item VAT rate (BT-152) shall be greater than zero.

```xpath
ram:RateApplicablePercent > 0
```

## EN16931-UBL-validation.xsl

> In an Invoice line (BG-25) where the Invoiced item VAT category code (BT-151) is "Standard rated" the Invoiced item VAT rate (BT-152) shall be greater than zero.

```xpath
(cbc:Percent) > 0
```

Shipped in cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 1 file(s) of the verification corpus, for example `mustang/Extended___SubInvoiceLines_Kaffee_Bundle_Set_Bsp4__.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
