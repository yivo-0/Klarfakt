---
rule: PEPPOL-EN16931-R120
severity: error
packs: [peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: []
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# PEPPOL-EN16931-R120

## PEPPOL-EN16931-UBL.xslt

> Invoice line net amount MUST equal (Invoiced quantity * (Item net price/item price base quantity) + Sum of invoice line charge amount - sum of invoice line allowance amount

```xpath
u:slack($lineExtensionAmount, ($quantity * ($priceAmount div $baseQuantity)) + $chargesTotal - $allowancesTotal, 0.02)
```

## XRechnung-CII-validation.xsl

> Invoice line net amount MUST equal (Invoiced quantity * (Item net price/item price base quantity) + Sum of invoice line charge amount - sum of invoice line allowance amount

```xpath
u:slack($lineExtensionAmount, ($quantity * ($priceAmount div $baseQuantity)) + $chargesTotal - $allowancesTotal, $slackValue)
```

Shipped in peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 1 file(s) of the verification corpus, for example `mustang/BT-128.ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
