using Klarfakt.Detection;
using Klarfakt.Validation;

namespace Klarfakt.Tests;

/// <summary>
/// "Valid" is only useful if it says valid against what. A document declaring a specification with
/// no rule set here was judged against EN 16931 and reported as valid, with nothing in the result
/// to say the rules it actually claimed to follow were never applied.
/// </summary>
[Collection("validator")]
public class ValidationContractTests(ValidatorFixture fixture)
{
    [Theory]
    [InlineData("urn:cen.eu:en16931:2017", true)]
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0", true)]
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0", true)]
    [InlineData("urn:factur-x.eu:1p0:basic", false)]
    [InlineData("urn:some.authority:cius:2029", false)]
    [InlineData(null, false)]
    // A national CIUS is declared by suffixing the EN 16931 identifier, so it parses as EN 16931
    // and used to be reported as fully covered. NLCIUS is the one in production use.
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:fdc:nen.nl:nlcius:v1.0", false)]
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:some.authority:cius:2029", false)]
    [InlineData("urn:cen.eu:en16931:2017#conformant#urn:some.authority:extension:1.0", false)]
    // One version of each pack is shipped. A name match is not a rule match.
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_99.0", false)]
    [InlineData("urn:fdc:peppol.eu:2017:poacc:billing:4.0", false)]
    // The XRechnung extension is covered because the shipped artefact carries its BR-DEX rules.
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0" +
                "#conformant#urn:xeinkauf.de:kosit:extension:xrechnung_3.0", true)]
    // Anything further appended is a specification of its own that nothing here judges. Reading the
    // version out of the middle of the identifier reported all three of these as fully covered.
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0" +
                "#conformant#urn:example:unsupported:1.0", false)]
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0" +
                "#conformant#urn:example:unsupported:1.0", false)]
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:xoev-de:kosit:standard:xrechnung_2.3", false)]
    public void Knows_which_declared_specifications_have_rules_of_their_own(string? identifier, bool covered)
    {
        Assert.Equal(covered, RulePackCatalog.Covers(InvoiceProfile.Parse(identifier), InvoiceSyntax.Ubl));
    }

    [Fact]
    public void The_covered_versions_match_the_packs_actually_shipped()
    {
        if (!fixture.Available) return;

        // Bumping a pack without revisiting Covers() would quietly start claiming coverage of a
        // version that is no longer the one on disk, or refusing the one that is.
        Assert.StartsWith(
            RulePackCatalog.XRechnungProfileVersion,
            fixture.Catalog!.Pack("xrechnung").Version,
            StringComparison.Ordinal);

        Assert.StartsWith(
            RulePackCatalog.PeppolProfileVersion,
            fixture.Catalog.Pack("peppol-bis").Version,
            StringComparison.Ordinal);

        // And not merely two constants agreeing with each other. Whatever version is on disk,
        // Covers has to accept the identifier a document declaring that version would carry —
        // which is the property the constants exist to protect, and the one that broke when
        // Covers stopped reading them.
        foreach (var identifier in new[]
        {
            $"{RulePackCatalog.En16931Identifier}#compliant#urn:xeinkauf.de:kosit:xrechnung_" +
            Profile(fixture.Catalog.Pack("xrechnung").Version),
            $"{RulePackCatalog.En16931Identifier}#compliant#urn:fdc:peppol.eu:2017:poacc:billing:" +
            Profile(fixture.Catalog.Pack("peppol-bis").Version),
        })
        {
            Assert.True(
                RulePackCatalog.Covers(InvoiceProfile.Parse(identifier), InvoiceSyntax.Ubl),
                $"a pack is restored whose own identifier Covers rejects: {identifier}");
        }
    }

    /// <summary>Packs are versioned to the patch; an identifier names the profile's major.minor.</summary>
    private static string Profile(string packVersion) =>
        string.Join('.', packVersion.Split('.').Take(2));

    [Fact]
    public void Strict_mode_refuses_an_extension_appended_to_a_covered_profile()
    {
        if (!fixture.Available) return;

        // Peppol 3.0 with something else layered on top. The base is covered, the whole identifier
        // is not, and reading the version out of the middle returned exit 0 and "valid" under
        // --strict for a specification no pack here has ever seen.
        var document = WithProfile(
            "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0" +
            "#conformant#urn:example:unsupported:1.0");

        Assert.False(RulePackCatalog.Covers(document.Profile, document.Syntax));

        var result = fixture.Validator!.Validate(document, strict: true);

        Assert.False(result.ProfileCovered);
        Assert.False(result.IsValid);
        Assert.Contains(result.Findings, finding => finding.RuleId == InvoiceValidator.ProfileRuleId);
    }

    [Fact]
    public void Strict_mode_refuses_a_national_cius_with_no_rules_here()
    {
        if (!fixture.Available) return;

        var document = WithProfile("urn:cen.eu:en16931:2017#compliant#urn:fdc:nen.nl:nlcius:v1.0");

        Assert.False(RulePackCatalog.Covers(document.Profile, document.Syntax));

        var result = fixture.Validator!.Validate(document, strict: true);

        Assert.False(result.ProfileCovered);
        Assert.Contains(result.Findings, finding => finding.RuleId == InvoiceValidator.ProfileRuleId);
    }

    [Fact]
    public void Reports_that_an_unknown_profile_fell_back_to_en16931()
    {
        if (!fixture.Available) return;

        var result = fixture.Validator!.Validate(WithProfile("urn:some.authority:cius:2029"));

        Assert.Equal(RuleSet.En16931, result.RuleSet);
        Assert.False(result.ProfileCovered);
        // The fallback verdict still stands on its own terms; it is the silence that was the problem.
        Assert.DoesNotContain(result.Findings, finding => finding.RuleId == InvoiceValidator.ProfileRuleId);
    }

    [Fact]
    public void Strict_mode_refuses_a_profile_it_has_no_rules_for()
    {
        if (!fixture.Available) return;

        var result = fixture.Validator!.Validate(WithProfile("urn:some.authority:cius:2029"), strict: true);

        Assert.False(result.IsValid);
        Assert.False(result.ProfileCovered);
        var finding = Assert.Single(result.Findings, candidate => candidate.RuleId == InvoiceValidator.ProfileRuleId);
        Assert.Equal(ValidationSeverity.Error, finding.Severity);
        Assert.Contains("urn:some.authority:cius:2029", finding.Message);
    }

    [Fact]
    public void Falls_back_for_a_peppol_invoice_in_a_syntax_the_pinned_pack_does_not_carry()
    {
        if (!fixture.Available) return;

        // Peppol BIS Billing CII Invoice is a registered Peppol document type, so this is ordinary
        // inbound traffic for anyone registered for CII. Choosing Peppol from the document and then
        // asking for artefacts the pinned configuration does not ship threw out of Validate, and
        // Parallel.ForEach turned that into an AggregateException that killed a whole folder.
        var document = CiiWithProfile(
            "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0");

        var result = fixture.Validator!.Validate(document);

        Assert.Equal(RuleSet.En16931, result.RuleSet);

        // And the coverage claim has to move with it. The identifier is one Klarfakt covers in UBL,
        // so reading the identifier alone reported this document as fully covered while no artefact
        // for it existed.
        Assert.False(result.ProfileCovered);
    }

    [Fact]
    public void Still_refuses_a_syntax_the_caller_named_rules_for()
    {
        if (!fixture.Available) return;

        // Falling back is for a rule set chosen from the document. A caller who names one has said
        // what this document should be judged against, and judging it against something else
        // quietly would be worse than saying no.
        Assert.Throws<RulePackException>(() => fixture.Validator!.Validate(
            Fixture.Load("facturx-cii.xml"), RuleSet.PeppolBisBilling3));
    }

    [Fact]
    public void Strict_mode_leaves_a_covered_profile_alone()
    {
        if (!fixture.Available) return;

        var result = fixture.Validator!.Validate(Fixture.Load("xrechnung-ubl.xml"), strict: true);

        Assert.True(result.ProfileCovered);
        Assert.True(result.IsValid);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void Strict_mode_defers_to_a_rule_set_the_caller_named()
    {
        if (!fixture.Available) return;

        // Naming a rule set is a decision; strict mode has nothing left to warn about.
        var result = fixture.Validator!.Validate(
            WithProfile("urn:some.authority:cius:2029"), RuleSet.En16931, strict: true);

        Assert.DoesNotContain(result.Findings, finding => finding.RuleId == InvoiceValidator.ProfileRuleId);
    }

    [Fact]
    public void Separates_a_schema_that_passed_from_one_that_was_never_checked()
    {
        if (!fixture.Available) return;

        var checked_ = fixture.Validator!.Validate(Fixture.Load("xrechnung-ubl.xml"));
        var skipped = fixture.Validator.Validate(Fixture.Load("xrechnung-ubl.xml"), validateSchema: false);

        Assert.True(checked_.SchemaChecked);
        Assert.True(checked_.SchemaValid);

        Assert.False(skipped.SchemaChecked);
        Assert.True(skipped.SchemaValid);
    }

    private static InvoiceDocument WithProfile(string identifier) => InvoiceDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "xrechnung-ubl.xml"))
            .Replace(
                "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0",
                identifier));

    private static InvoiceDocument CiiWithProfile(string identifier) => InvoiceDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "facturx-cii.xml"))
            .Replace("urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic", identifier));
}
