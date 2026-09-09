---
rule: BR-CO-15
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: [BT-109, BT-110, BT-112]
corpus_hits: 2
# Flip to true once the sections below are written.
published: false
---

# BR-CO-15

## EN16931-CII-validation.xslt

> Invoice total amount with VAT (BT-112) = Invoice total amount without VAT (BT-109) + Invoice total VAT amount (BT-110).

```xpath
every $Currency in /rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeSettlement/ram:InvoiceCurrencyCode satisfies ( ( count(/rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeSettlement/ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:TaxTotalAmount[@currencyID = $Currency]) = 1 and xs:decimal((/rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeSettlement/ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:GrandTotalAmount)[1]) = round( ( xs:decimal((/rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeSettlement/ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:TaxBasisTotalAmount)[1]) + xs:decimal((/rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeSettlement/ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:TaxTotalAmount[@currencyID = $Currency])[1]) ) * 100 ) div 100 ) or ( xs:decimal((/rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeSettlement/ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:GrandTotalAmount)[1]) = xs:decimal((/rsm:CrossIndustryInvoice/rsm:SupplyChainTradeTransaction/ram:ApplicableHeaderTradeSettlement/ram:SpecifiedTradeSettlementHeaderMonetarySummation/ram:TaxBasisTotalAmount)[1]) ) )
```

## EN16931-UBL-validation.xslt

> Invoice total amount with VAT (BT-112) = Invoice total amount without VAT (BT-109) + Invoice total VAT amount (BT-110).

```xpath
every $Currency in cbc:DocumentCurrencyCode satisfies (count(cac:TaxTotal/xs:decimal(cbc:TaxAmount[@currencyID=$Currency])) eq 1) and (cac:LegalMonetaryTotal/xs:decimal(cbc:TaxInclusiveAmount) = round( (cac:LegalMonetaryTotal/xs:decimal(cbc:TaxExclusiveAmount) + cac:TaxTotal/xs:decimal(cbc:TaxAmount[@currencyID=$Currency])) * 10 * 10) div 100)
```

Shipped in cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 2 file(s) of the verification corpus, for example `mustang/BT-128.ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
