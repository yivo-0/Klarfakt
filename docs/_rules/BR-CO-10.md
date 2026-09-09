---
rule: BR-CO-10
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: [BT-106, BT-131]
corpus_hits: 10
# Flip to true once the sections below are written.
published: false
---

# BR-CO-10

## EN16931-CII-validation.xslt

> Sum of Invoice line net amount (BT-106) = Σ Invoice line net amount (BT-131).

```xpath
xs:decimal(ram:LineTotalAmount) = round(xs:decimal(sum(../../ram:IncludedSupplyChainTradeLineItem/ram:SpecifiedLineTradeSettlement/ram:SpecifiedTradeSettlementLineMonetarySummation/ram:LineTotalAmount)) * xs:decimal(100)) div xs:decimal(100)
```

## EN16931-UBL-validation.xslt

> Sum of Invoice line net amount (BT-106) = Σ Invoice line net amount (BT-131).

```xpath
(xs:decimal(cbc:LineExtensionAmount) = xs:decimal(round(sum(//(cac:InvoiceLine|cac:CreditNoteLine)/xs:decimal(cbc:LineExtensionAmount)) * 10 * 10) div 100))
```

Shipped in cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 10 file(s) of the verification corpus, for example `facturx/factur-x-basicwl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
