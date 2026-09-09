---
rule: PEPPOL-EN16931-R001
severity: error
packs: [peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: []
corpus_hits: 2
# Flip to true once the sections below are written.
published: false
---

# PEPPOL-EN16931-R001

## PEPPOL-EN16931-UBL.xslt

> Business process MUST be provided.

```xpath
cbc:ProfileID
```

## XRechnung-CII-validation.xsl

> Business process MUST be provided.

```xpath
ram:BusinessProcessSpecifiedDocumentContextParameter/ram:ID
```

Shipped in peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 2 file(s) of the verification corpus, for example `mustang/ubl-selfbilled-01.20a-INVOICE_ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
