---
rule: BR-CO-12
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: [BT-99, BT-108]
corpus_hits: 2
# Flip to true once the sections below are written.
published: false
---

# BR-CO-12

## EN16931-CII-validation.xslt

> Sum of charges on document level (BT-108) = Σ Document level charge amount (BT-99).

```xpath
(not(/rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeSettlement/ram:SpecifiedTradeAllowanceCharge[ram:ChargeIndicator/udt:Indicator=true()])and not (ram:ChargeTotalAmount)) or ram:ChargeTotalAmount = (round(sum(/rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeSettlement/ram:SpecifiedTradeAllowanceCharge[ram:ChargeIndicator/udt:Indicator=true()]/ram:ActualAmount[1])* 10 * 10 ) div 100)
```

## EN16931-UBL-validation.xslt

> Sum of charges on document level (BT-108) = Σ Document level charge amount (BT-99).

```xpath
xs:decimal(cbc:ChargeTotalAmount) = (round(sum(../cac:AllowanceCharge[cbc:ChargeIndicator=true()]/xs:decimal(cbc:Amount)) * 10 * 10) div 100) or (not(cbc:ChargeTotalAmount) and not(../cac:AllowanceCharge[cbc:ChargeIndicator=true()]))
```

Shipped in cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 2 file(s) of the verification corpus, for example `mustang/extended_warenrechnung_based_doublecashdiscount.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
