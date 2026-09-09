---
rule: PEPPOL-EN16931-R110
severity: error
packs: [peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: []
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# PEPPOL-EN16931-R110

## PEPPOL-EN16931-UBL.xslt

> Start date of line period MUST be within invoice period.

## XRechnung-CII-validation.xsl

> Start date of line period MUST be within invoice period.

```xpath
udt:DateTimeString >= ../../../../ram:ApplicableHeaderTradeSettlement/ram:BillingSpecifiedPeriod/ram:StartDateTime/udt:DateTimeString
```

## XRechnung-UBL-validation.xsl

> Start date of line period MUST be within invoice period.

```xpath
xs:date(text()) >= xs:date(../../../cac:InvoicePeriod/cbc:StartDate)
```

Shipped in peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 1 file(s) of the verification corpus, for example `mustang/testout-ZF2PushEdge.ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
