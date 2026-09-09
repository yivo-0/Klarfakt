---
rule: BR-CO-16
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: [BT-112, BT-113, BT-114, BT-115]
corpus_hits: 3
# Flip to true once the sections below are written.
published: false
---

# BR-CO-16

## EN16931-CII-validation.xslt

> Amount due for payment (BT-115) = Invoice total amount with VAT (BT-112) -Paid amount (BT-113) +Rounding amount (BT-114).

```xpath
(xs:decimal(ram:DuePayableAmount[1]) = xs:decimal(ram:GrandTotalAmount[1]) - xs:decimal(ram:TotalPrepaidAmount[1]) + xs:decimal(ram:RoundingAmount[1])) or ((xs:decimal(ram:DuePayableAmount[1]) = xs:decimal(ram:GrandTotalAmount[1]) + xs:decimal(ram:RoundingAmount[1])) and not(ram:TotalPrepaidAmount)) or ((xs:decimal(ram:DuePayableAmount[1]) = xs:decimal(ram:GrandTotalAmount[1]) - xs:decimal(ram:TotalPrepaidAmount[1])) and not(ram:RoundingAmount)) or ((xs:decimal(ram:DuePayableAmount[1]) = xs:decimal(ram:GrandTotalAmount[1])) and not(ram:TotalPrepaidAmount) and not(ram:RoundingAmount))
```

## EN16931-UBL-validation.xslt

> Amount due for payment (BT-115) = Invoice total amount with VAT (BT-112) -Paid amount (BT-113) +Rounding amount (BT-114).

```xpath
(exists(cbc:PrepaidAmount) and not(exists(cbc:PayableRoundingAmount)) and (xs:decimal(cbc:PayableAmount) = (round((xs:decimal(cbc:TaxInclusiveAmount) - xs:decimal(cbc:PrepaidAmount)) * 10 * 10) div 100))) or (not(exists(cbc:PrepaidAmount)) and not(exists(cbc:PayableRoundingAmount)) and xs:decimal(cbc:PayableAmount) = xs:decimal(cbc:TaxInclusiveAmount)) or (exists(cbc:PrepaidAmount) and exists(cbc:PayableRoundingAmount) and ((round((xs:decimal(cbc:PayableAmount) - xs:decimal(cbc:PayableRoundingAmount)) * 10 * 10) div 100) = (round((xs:decimal(cbc:TaxInclusiveAmount) - xs:decimal(cbc:PrepaidAmount)) * 10 * 10) div 100))) or (not(exists(cbc:PrepaidAmount)) and exists(cbc:PayableRoundingAmount) and ((round((xs:decimal(cbc:PayableAmount) - xs:decimal(cbc:PayableRoundingAmount)) * 10 * 10) div 100) = xs:decimal(cbc:TaxInclusiveAmount)))
```

Shipped in cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 3 file(s) of the verification corpus, for example `kosit-xrechnung/05.01a-INVOICE_ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
