using Klarfakt.Detection;
using Klarfakt.Validation;

namespace Klarfakt.Tests;

/// <summary>
/// Factur-X MINIMUM and BASIC WL carry less than EN 16931 requires, and say so by leaving the CEN
/// identifier off. Judging one against the core rules reported the same handful of errors every
/// time and called the file invalid — a statement about the profile rather than about the invoice,
/// and one that failed a whole archive for containing an ordinary French invoice.
/// </summary>
[Collection("validator")]
public class UncoveredProfileTests(ValidatorFixture fixture) : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("klarfakt-uncovered").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Theory]
    [InlineData("urn:cen.eu:en16931:2017", true)]
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic", true)]
    [InlineData("urn:cen.eu:en16931:2017#conformant#urn:factur-x.eu:1p0:extended", true)]
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0", true)]
    // A national CIUS with no rule set here still claimed conformance, so the core rules are the
    // right ones to judge it by.
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:fdc:nen.nl:nlcius:v1.0", true)]
    [InlineData("  urn:cen.eu:en16931:2017  ", true)]
    [InlineData("urn:factur-x.eu:1p0:minimum", false)]
    [InlineData("urn:factur-x.eu:1p0:basicwl", false)]
    [InlineData("urn:some.authority:cius:2029", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Knows_whether_an_identifier_claims_en16931(string? identifier, bool asserts)
    {
        Assert.Equal(asserts, InvoiceProfile.Parse(identifier).AssertsEn16931);
    }

    [Fact]
    public void A_profile_that_never_claimed_en16931_is_neither_valid_nor_invalid()
    {
        if (!fixture.Available) return;

        var report = Assert.Single(Run("urn:factur-x.eu:1p0:minimum"));

        Assert.Equal("uncovered", report.Status);
        Assert.False(report.ProfileCovered);
    }

    [Fact]
    public void A_rule_set_the_caller_named_is_a_verdict_that_was_asked_for()
    {
        if (!fixture.Available) return;

        var report = Assert.Single(Run("urn:factur-x.eu:1p0:minimum", ruleSet: RuleSet.En16931));

        Assert.NotEqual("uncovered", report.Status);
    }

    [Fact]
    public void Strict_mode_turns_an_unjudged_document_into_a_failure()
    {
        if (!fixture.Available) return;

        var report = Assert.Single(Run("urn:factur-x.eu:1p0:minimum", strict: true));

        Assert.Equal("invalid", report.Status);
    }

    [Fact]
    public void A_cius_that_did_claim_en16931_keeps_its_verdict()
    {
        if (!fixture.Available) return;

        var report = Assert.Single(Run("urn:cen.eu:en16931:2017#compliant#urn:fdc:nen.nl:nlcius:v1.0"));

        // No rule set here either, so the fallback still has to be reported — but the document
        // asked to be measured against EN 16931, and that verdict is earned.
        Assert.False(report.ProfileCovered);
        Assert.NotEqual("uncovered", report.Status);
    }

    [Fact]
    public void A_document_that_fails_its_schema_is_invalid_whatever_it_declares()
    {
        if (!fixture.Available) return;

        // A structural defect owes nothing to the profile, and the business rules never ran, so
        // there is nothing here for "no verdict" to describe.
        var path = Path.Combine(_root, "broken.xml");
        File.WriteAllText(path, Fixture("urn:factur-x.eu:1p0:minimum")
            .Replace("<ram:ID>", "<ram:NotAnElement/><ram:ID>"));

        var report = Assert.Single(Batch.Run(
            fixture.Validator!, [new InvoiceFile(path, "broken.xml")], null, validateSchema: true));

        Assert.False(report.SchemaValid);
        Assert.Equal("invalid", report.Status);
    }

    [Fact]
    public void The_published_minimum_and_basic_wl_invoices_are_not_judged()
    {
        if (!fixture.Available || Corpus.Root is null) return;

        var directory = Path.Combine(Corpus.Root, "facturx");
        if (!Directory.Exists(directory)) return;

        var files = Corpus.Files(directory, ".xml")
            .Where(file => Path.GetFileName(file) is "factur-x-minimum.xml" or "factur-x-basicwl.xml")
            .Select(file => new InvoiceFile(file, Path.GetFileName(file)))
            .ToList();

        Assert.Equal(2, files.Count);

        // Seven and five guaranteed EN 16931 errors respectively, and an archive that exited 1.
        Assert.All(
            Batch.Run(fixture.Validator!, files, null, validateSchema: true),
            report => Assert.Equal("uncovered", report.Status));
    }

    [Fact]
    public void The_summary_counts_unjudged_files_apart_from_the_rest()
    {
        var reports = new List<Report>
        {
            new("a.xml", "valid", "XRechnung", true, true, true, "urn:xrechnung", null, [], null),
            new("b.xml", "uncovered", "En16931", true, true, false,
                "urn:factur-x.eu:1p0:minimum", null, [], null),
        };

        using var writer = new StringWriter();
        Batch.WriteSummary(writer, reports);
        var summary = writer.ToString();

        Assert.Contains("2 file(s): 1 valid, 0 with errors, 1 not judged, 0 unreadable", summary);
        Assert.Contains("does not claim EN 16931 conformance", summary);

        // The fallback line counts what the line above has not, so one document does not read as
        // two separate problems.
        Assert.DoesNotContain("judged against EN 16931 alone", summary);
    }

    [Fact]
    public void The_ranking_leaves_out_the_errors_nobody_reached_a_verdict_on()
    {
        var guaranteed = new Finding(
            "BR-CO-10", nameof(ValidationSeverity.Error), "sum of line amounts", "/x", null, [], "artefact.xsl");

        var reports = new List<Report>
        {
            new("a.xml", "uncovered", "En16931", true, true, false,
                "urn:factur-x.eu:1p0:minimum", null, [guaranteed], null),
        };

        using var writer = new StringWriter();
        Batch.WriteSummary(writer, reports);
        var summary = writer.ToString();

        // "0 with errors" over a ranked list of ten rules was the archive summary before this.
        Assert.Contains("1 file(s): 0 valid, 0 with errors, 1 not judged, 0 unreadable", summary);
        Assert.DoesNotContain("Most frequent errors", summary);
        Assert.DoesNotContain("BR-CO-10", summary);
    }

    private List<Report> Run(string identifier, RuleSet? ruleSet = null, bool strict = false)
    {
        var path = Path.Combine(_root, "invoice.xml");
        File.WriteAllText(path, Fixture(identifier));

        return Batch.Run(
            fixture.Validator!,
            [new InvoiceFile(path, "invoice.xml")],
            ruleSet,
            validateSchema: true,
            strict);
    }

    private static string Fixture(string identifier) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "facturx-cii.xml"))
            .Replace("urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic", identifier);
}
