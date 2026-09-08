---
layout: default
title: "Commercial licence"
heading: "Commercial licence agreement"
description: "The terms a paid Klarfakt licence grants: what you may do with it, what you keep when it lapses, and what is not warranted."
permalink: /commercial-licence/
---

These are the terms of a paid Klarfakt licence. They apply in addition to the
[Business Source License 1.1](https://github.com/yivo-0/Klarfakt/blob/main/LICENSE)
that ships with the software, and they replace its production-use restriction for
the term you have paid for.

Most readers do not need this page. Klarfakt is free below the revenue threshold,
free inside OSI-licensed open source, and free always for evaluation, development
and CI — see [pricing]({{ '/pricing/' | relative_url }}).

## 1. Parties

**Licensor:** Oleh Yanytskyi, Ukraine, contact licensing@klarfakt.dev.

**Licensee:** the organisation named on the invoice. "Affiliates" means entities
that Licensee controls, is controlled by, or is under common control with, where
control means more than 50% of voting rights.

## 2. What is licensed

**"Software"** means the Klarfakt library and the `klarfakt` command line tool,
in the versions published by Licensor to nuget.org.

**"Rule artefacts"** means the validation files the Software downloads on request
from CEN, OpenPEPPOL and KoSIT. **They are not licensed under this agreement.**
Licensor does not own or redistribute them; they are published by their authors
under EUPL-1.2 and Apache-2.0 and your use of them is governed by those licences.
The Software fetches them from the publishers and verifies each against a
recorded SHA-256.

## 3. Grant

Subject to payment, Licensor grants Licensee a non-exclusive, non-transferable,
worldwide licence for the Term.

**Standard** — use, copy and install the Software within the business operations
of Licensee and its Affiliates. This includes running it inside a service that
Licensee operates for its own customers, with unlimited developers, deployments
and validations. It does **not** include giving the Software itself to anyone
else.

**Extended** — everything in Standard, and additionally the right to incorporate
the Software into a product Licensee develops and to distribute it, in compiled
form, as an integrated component of that product. Licensee's customers receive no
right to use the Software separately from that product, and must be bound by
terms at least as protective of Licensor as these.

If you ship Klarfakt inside something you sell, you need Extended. That is the
whole difference between the tiers.

## 4. What a licence keeps

**Every version released during the Term may be used indefinitely**, in
production, under the grant above — including after the Term ends. This survives
termination and non-renewal.

What ends with the Term is access to *new* versions, new rule pack versions, and
support. Nothing already in your pipeline stops working, and nothing has to be
removed.

## 5. Restrictions

Licensee shall not:

1. sublicense, resell or distribute the Software, except as clause 3 permits
   under Extended;
2. remove or alter copyright, licence or attribution notices;
3. use the Software to develop a substantially similar invoice validation library
   for distribution to third parties;
4. represent a Klarfakt verdict as certification, accreditation or approval by
   Licensor, CEN, OpenPEPPOL, KoSIT or any authority.

There is no technical licence enforcement. The Software does not phone home, does
not count invoices and is not built to. Compliance is on your honour, and the
tiers are written so that the honest answer is also the obvious one.

## 6. Support

Email support from the maintainer at the address above. Extended adds a named
contact and places Licensee's issues ahead of the backlog.

Response times are described on the [pricing page]({{ '/pricing/' | relative_url }})
rather than committed here, because a commitment nobody can keep on a bad week is
worth less than none.

## 7. What Licensor warrants

Licensor warrants that, during the Term:

1. the Software will perform materially as its documentation describes; and
2. where the Software's verdict on a document differs from that of the reference
   implementation it is compared against — KoSIT's validationtool, running the
   same rule artefact release — **Licensor will treat that difference as a defect
   in the Software.**

The second warranty is the product. It is verified on every build and the result
is published.

Licensee's remedy for a breach of this clause is correction within a reasonable
period or, if Licensor does not correct it, a refund of the fees paid for the
current Term. That is the entire remedy for breach of warranty.

## 8. What Licensor does not warrant

**A verdict is not advice.** Klarfakt is software, not a tax adviser, accountant
or lawyer. Nothing it reports is legal, tax or accounting advice.

**A verdict is not an acceptance guarantee.** Klarfakt reports what the
publishers' rule artefacts report. It cannot and does not guarantee that any tax
authority, trading partner, access point or auditor will accept a document it
reports as valid, or reject one it reports as invalid. Those parties apply their
own rules, versions and judgement.

**Rule artefacts are third-party work.** Licensor does not warrant their
correctness, completeness or fitness. Where they are wrong, Klarfakt reproduces
them faithfully, which is the intended behaviour.

Except as stated in clause 7, the Software is provided as is, and all other
warranties, express or implied, are excluded to the extent the law permits.

## 9. Liability

Nothing in this agreement limits liability for intent or gross negligence, for
injury to life, body or health, or under mandatory product liability law.

For simple negligence, Licensor is liable only for breach of an obligation whose
fulfilment is essential to performing this agreement and on which Licensee may
reasonably rely, and then only for damage that is foreseeable and typical for
this kind of contract.

In every case, Licensor's total liability is capped at the fees Licensee paid in
the twelve months before the event giving rise to it.

Licensor is not liable for indirect or consequential loss, lost profit, lost
data, or for fines, interest or penalties imposed on Licensee by a third party,
including any tax authority.

Licensee remains responsible for its own compliance obligations. Klarfakt is a
tool used in meeting them, not a transfer of them.

## 10. Fees and VAT

Fees are annual, payable in advance against invoice, and exclusive of VAT.

Licensor is established in Ukraine. For business customers in the European Union,
VAT is accounted for by the customer under the reverse charge (Article 196,
Directive 2006/112/EC). Licensee shall provide a valid VAT identification number
and confirm its business status; where it does not, Licensor may charge VAT if
required to.

Fees are non-refundable except under clause 7.

## 11. Term, renewal and termination

The Term is one year from the invoice date. **It does not renew automatically.**
Licensor will offer renewal before expiry; if it is not renewed, clause 4 governs
what Licensee keeps.

Either party may terminate for material breach not cured within 30 days of
written notice. Licensor may terminate immediately for a breach of clause 5.

On termination, clauses 4, 5, 8, 9 and 12 survive.

## 12. General

**Governing law.** German law, excluding the UN Convention on Contracts for the
International Sale of Goods. Place of jurisdiction is Frankfurt am Main, Germany,
for disputes with merchants.

**Assignment.** Neither party may assign this agreement without the other's
written consent, except to a successor of substantially all of its business.
Consent shall not be unreasonably withheld.

**Entire agreement.** These terms, together with the invoice, are the whole
agreement. Licensee's own purchasing terms do not apply unless Licensor has
accepted them in writing.

**Severability.** If a provision is unenforceable, the rest stands.

**Changes.** Licensor may change these terms for future Terms. The terms in force
when a Term begins govern that Term.

---

Questions before buying are welcome and usually faster than a redline:
licensing@klarfakt.dev.
