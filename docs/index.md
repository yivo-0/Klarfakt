---
layout: default
title: Klarfakt
heading: EN 16931 e-invoice validation for .NET
description: Read and validate EN 16931 electronic invoices in .NET. No Java, no Node, nothing leaves your server.
permalink: /
---

Since January 2025 every company in Germany must be able to receive an electronic invoice. If your
software receives them, someone will send you a file and expect you to know whether it is valid.

Klarfakt does that in .NET, in your own process. It runs the publishers' **own compiled Schematron** —
the same artefacts the German reference validator executes — fetched from their pinned upstream
releases and checked against a recorded SHA-256. Not a reimplementation of the rules, and not a copy
vendored into a repository where it can drift.

## 97 of 97

Every CI run puts 143 invoices through both Klarfakt and [KoSIT's validationtool][kosit], using the
same scenario configuration release, and diffs the rule ids each reports.

**97 of 97 comparable files agree rule for rule.** The other 46 are recorded with the reason they
cannot be compared. A disagreement that is not written down fails the build.

That is the claim worth checking, and [the script that proves it][diff] is in the repository.

## What it covers

| | |
|---|---|
| **Syntaxes** | UBL 2.1 Invoice and CreditNote, CII D16B, and the XML inside a hybrid Factur-X / ZUGFeRD 2.x PDF |
| **Rule sets** | EN 16931, Peppol BIS Billing 3.0, XRechnung 3.0.2 |
| **Render** | a self-contained HTML page from KoSIT's official XRechnung visualisation |
| **Frameworks** | net8.0, net10.0 |

```bash
dotnet add package Klarfakt --version 1.0.0-preview.3
```

```csharp
var validator = new InvoiceValidator();
var document = InvoiceDocument.Load("invoice.xml");   // or a Factur-X / ZUGFeRD PDF
var result = validator.Validate(document);

foreach (var error in result.Errors)
{
    Console.WriteLine($"{error.RuleId} at {error.Location}: {error.Message}");
}
```

Findings carry the rule id, severity, location, the failing XPath test, the business terms, the
offending value where the rule pointed at one, and which artefact produced them — so a CEN core rule
is distinguishable from a German CIUS rule.

## The rule artefacts are not redistributed

The CEN rules are EUPL-1.2 and the KoSIT configurations are Apache-2.0. They stay with their
publishers and are downloaded on request, over plain HTTPS from a pinned release, each file checked
against the SHA-256 in the embedded manifest before it is written and again before first use.

Nothing else in Klarfakt touches the network. Validation is entirely local, and a Docker build can
bake the artefacts into the image so the container runs with no network at all.

## The 2026 revision

CEN revised EN 16931 in 2026, and the two editions run in parallel until 31 March 2029. No
XRechnung, Peppol BIS or CEN artefact exists for the new model yet — the profiles are still CIUSes
of the 2017 one. [What that means for a licence]({{ '/en16931-2026/' | relative_url }}).

## Writing

{% for post in site.posts limit: 3 %}
- [{{ post.title }}]({{ post.url | relative_url }}) — {{ post.description }}
{% endfor %}

[All writing]({{ '/blog/' | relative_url }}) · [Atom feed]({{ '/feed.xml' | relative_url }})

## Licence

**Free unless your organisation turns over more than €1,000,000 a year.** Free at any size inside
OSI-licensed open source. Free always for evaluation, development, CI and internal testing.

Above that threshold and in production, a commercial licence is **€690 a year** — [pricing]({{ '/pricing/' | relative_url }}).
Every version becomes Apache 2.0 four years after it ships, so there is no lock-in if the project
stops being maintained.

[Source on GitHub](https://github.com/yivo-0/Klarfakt) · licensing@klarfakt.dev

[kosit]: https://github.com/itplr-kosit/validator
[diff]: https://github.com/yivo-0/Klarfakt/blob/main/tools/reference-diff.py
