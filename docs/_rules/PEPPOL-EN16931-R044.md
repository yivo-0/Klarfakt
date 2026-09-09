---
rule: PEPPOL-EN16931-R044
severity: error
packs: [peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: []
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# PEPPOL-EN16931-R044

## PEPPOL-EN16931-UBL.xslt

> Charge on price level is NOT allowed. Only value 'false' allowed.

```xpath
normalize-space(cbc:ChargeIndicator) = 'false'
```

## XRechnung-CII-validation.xsl

> Charge on price level is NOT allowed. Only value 'false' allowed.

```xpath
not(ram:AppliedTradeAllowanceCharge/ram:ActualAmount) or ram:AppliedTradeAllowanceCharge/ram:ChargeIndicator/udt:Indicator = 'false'
```

Shipped in peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 1 file(s) of the verification corpus, for example `mustang/testout-ZF2PushItemChargesAllowances.ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
