---
rule: BR-CO-13
severity: error
packs: [cen-en16931 1.3.16, peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: [BT-107, BT-108, BT-109, BT-131]
corpus_hits: 2
# Flip to true once the sections below are written.
published: false
---

# BR-CO-13

## EN16931-CII-validation.xslt

> Invoice total amount without VAT (BT-109) = Σ Invoice line net amount (BT-131) - Sum of allowances on document level (BT-107) + Sum of charges on document level (BT-108).

```xpath
(xs:decimal(ram:TaxBasisTotalAmount[1]) = round((xs:decimal(ram:LineTotalAmount[1]) - xs:decimal(ram:AllowanceTotalAmount[1]) + xs:decimal(ram:ChargeTotalAmount[1])) * 10 * 10) div 100) or ((xs:decimal(ram:TaxBasisTotalAmount[1]) = round((xs:decimal(ram:LineTotalAmount[1]) - xs:decimal(ram:AllowanceTotalAmount[1])) * 10 * 10) div 100) and not(ram:ChargeTotalAmount)) or ((xs:decimal(ram:TaxBasisTotalAmount[1]) = round((xs:decimal(ram:LineTotalAmount[1]) + xs:decimal(ram:ChargeTotalAmount[1])) * 10 * 10) div 100) and not(ram:AllowanceTotalAmount)) or ((xs:decimal(ram:TaxBasisTotalAmount[1]) = round((xs:decimal(ram:LineTotalAmount[1])) * 10 * 10) div 100) and not(ram:ChargeTotalAmount) and not(ram:AllowanceTotalAmount))
```

## EN16931-UBL-validation.xslt

> Invoice total amount without VAT (BT-109) = Σ Invoice line net amount (BT-131) - Sum of allowances on document level (BT-107) + Sum of charges on document level (BT-108).

```xpath
((cbc:ChargeTotalAmount) and (cbc:AllowanceTotalAmount) and (xs:decimal(cbc:TaxExclusiveAmount) = round((xs:decimal(cbc:LineExtensionAmount) + xs:decimal(cbc:ChargeTotalAmount) - xs:decimal(cbc:AllowanceTotalAmount)) * 10 * 10) div 100 )) or (not(cbc:ChargeTotalAmount) and (cbc:AllowanceTotalAmount) and (xs:decimal(cbc:TaxExclusiveAmount) = round((xs:decimal(cbc:LineExtensionAmount) - xs:decimal(cbc:AllowanceTotalAmount)) * 10 * 10 ) div 100)) or ((cbc:ChargeTotalAmount) and not(cbc:AllowanceTotalAmount) and (xs:decimal(cbc:TaxExclusiveAmount) = round((xs:decimal(cbc:LineExtensionAmount) + xs:decimal(cbc:ChargeTotalAmount)) * 10 * 10 ) div 100)) or (not(cbc:ChargeTotalAmount) and not(cbc:AllowanceTotalAmount) and (xs:decimal(cbc:TaxExclusiveAmount) = xs:decimal(cbc:LineExtensionAmount)))
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
