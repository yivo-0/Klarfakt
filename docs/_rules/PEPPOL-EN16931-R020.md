---
rule: PEPPOL-EN16931-R020
severity: error
packs: [peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: []
corpus_hits: 14
# Flip to true once the sections below are written.
published: false
---

# PEPPOL-EN16931-R020

## PEPPOL-EN16931-UBL.xslt

> Seller electronic address MUST be provided

```xpath
cbc:EndpointID
```

## XRechnung-CII-validation.xsl

> Seller electronic address MUST be provided

```xpath
ram:URIUniversalCommunication/ram:URIID
```

Shipped in peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 14 file(s) of the verification corpus, for example `mustang/XRECHNUNG_Betriebskostenabrechnung.ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
