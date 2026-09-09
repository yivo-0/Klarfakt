---
rule: BR-CO-14
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: [BT-110, BT-117]
corpus_hits: 2
# Flip to true once the sections below are written.
published: false
---

# BR-CO-14

## EN16931-CII-validation.xslt

> Invoice total VAT amount (BT-110) = Σ VAT category tax amount (BT-117).

```xpath
. = (round(sum(/rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeSettlement/ram:ApplicableTradeTax/ram:CalculatedAmount)*10*10)div 100)
```

## EN16931-UBL-validation.xslt

> Invoice total VAT amount (BT-110) = Σ VAT category tax amount (BT-117).

```xpath
(xs:decimal(child::cbc:TaxAmount)= round((sum(cac:TaxSubtotal/xs:decimal(cbc:TaxAmount)) * 10 * 10)) div 100) or not(cac:TaxSubtotal)
```

Shipped in cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 2 file(s) of the verification corpus, for example `facturx/factur-x-minimum.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
