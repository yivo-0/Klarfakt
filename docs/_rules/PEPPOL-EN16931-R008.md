---
rule: PEPPOL-EN16931-R008
severity: error
packs: [peppol-bis 3.0.21, xrechnung 3.0.2]
business_terms: []
corpus_hits: 1
# Flip to true once the sections below are written.
published: false
---

# PEPPOL-EN16931-R008

## PEPPOL-EN16931-UBL.xslt

> Document MUST not contain empty elements.

```xpath
false()
```

## XRechnung-UBL-validation.xsl

> Document MUST not contain empty elements.

```xpath
self::cbc:ID[parent::cac:OrderReference]
```

Shipped in peppol-bis 3.0.21, xrechnung 3.0.2.

Fired on 1 file(s) of the verification corpus, for example `mustang/XRECHNUNG_Elektron.ubl.xml`.

<!-- Written by hand below this line. tools/rule-pages.py never touches it. -->

## What it means

TODO

## Why it fires

TODO

## How to fix it

TODO
