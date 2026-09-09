using Klarfakt.Rendering;
using Klarfakt.Validation;

namespace Klarfakt.Tests;

/// <summary>
/// When an artefact is read decides whether a rule pack can be replaced under a running process.
/// A validator reads a stylesheet while it compiles it and keeps the compiled form, not the file,
/// so the directory can go and it carries on. The renderer is not the same: the publisher's HTML
/// stylesheet pulls its CSS, its script and its label catalogue in with <c>unparsed-text()</c>,
/// which is evaluated on every page.
/// </summary>
/// <remarks>
/// Issue #15 read an intermittent cleanup failure as Saxon holding the artefacts for the
/// validator's lifetime and proposed <c>IDisposable</c>. Measured: a compile leaves a handle on
/// roughly one file in twenty and it is released by the finalizer, identically whether the source
/// is a Uri or a stream this library owns and closes — so it is not ours to take differently, and
/// no Saxon type offers Dispose or Close for the interface to call. What replaces the API is
/// <see cref="DeleteRules"/>: a collection, then try again.
/// </remarks>
[Collection("validator")]
public class ArtefactLifetimeTests(ValidatorFixture fixture) : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("klarfakt-lifetime").FullName;

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) DeleteRules(_root);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    [Fact]
    public void Keeps_validating_after_the_artefacts_it_compiled_are_deleted()
    {
        if (!fixture.Available) return;

        var validator = new InvoiceValidator(CopyOfTheRules());
        var document = Fixture.Load("xrechnung-ubl.xml");

        // Warmed up as well as used, so every artefact it could reach has been compiled.
        Assert.True(validator.Validate(document).IsValid);
        validator.Warmup();

        DeleteRules(_root);

        Assert.True(validator.Validate(document).IsValid);
    }

    [Fact]
    public void Rendering_reads_the_publishers_css_and_script_on_every_page()
    {
        if (!fixture.Available) return;

        var catalog = CopyOfTheRules();
        var renderer = new InvoiceRenderer(catalog);
        var validator = new InvoiceValidator(catalog);
        var document = Fixture.Load("xrechnung-ubl.xml");

        Assert.NotEmpty(renderer.ToHtml(document));
        Assert.True(validator.Validate(document).IsValid);
        renderer.Warmup();

        DeleteRules(_root);

        // Compiling the stylesheet is not enough to make the renderer independent of the pack: the
        // page it produces embeds xrechnung-viewer.css, the viewer script and FileSaver, and the
        // stylesheet goes to disk for them every time it runs. Validation alongside it is
        // unaffected, which is what makes this the visualisation pack's constraint and not the
        // catalog's.
        Assert.Throws<RenderingException>(() => renderer.ToHtml(document));
        Assert.True(validator.Validate(document).IsValid);
    }

    [Fact]
    public void A_new_validator_can_replace_the_artefacts_the_old_one_compiled()
    {
        if (!fixture.Available) return;

        // The whole of what "swap rule packs at runtime" needs: restore over the directory and
        // build another validator. Releasing the old one is dropping the reference.
        var first = new InvoiceValidator(CopyOfTheRules());
        var document = Fixture.Load("xrechnung-ubl.xml");
        Assert.True(first.Validate(document).IsValid);

        DeleteRules(_root);
        var second = new InvoiceValidator(CopyOfTheRules());

        Assert.True(second.Validate(document).IsValid);
        Assert.True(first.Validate(document).IsValid);
    }

    /// <summary>
    /// Deletes a rules directory the way a process replacing a pack has to. A stylesheet Saxon
    /// compiled is occasionally still open, and a collection is what closes it — measured at
    /// 60 of 60 across runs where the first attempt failed, and never released without one.
    /// </summary>
    private static void DeleteRules(string root)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                Directory.Delete(root, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 4)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }

    private RulePackCatalog CopyOfTheRules()
    {
        foreach (var source in Directory.EnumerateFiles(fixture.Catalog!.Root, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(_root, Path.GetRelativePath(fixture.Catalog.Root, source));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination);
        }

        return RulePackCatalog.Load(_root);
    }
}
