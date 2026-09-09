using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text;
using Klarfakt;
using Klarfakt.Validation;

/// <summary>
/// Validating a folder of invoices. Aimed at pointing Klarfakt at an existing archive to find out
/// how much of it an access point would reject.
/// </summary>
internal static class Batch
{
    private static readonly string[] Extensions = [".xml", ".pdf"];

    /// <summary>
    /// Expands directories to the invoice files inside them; plain paths pass through. The path each
    /// file had relative to the folder it was found in is kept, so a command writing output per
    /// invoice can reproduce the folder structure instead of flattening it.
    /// </summary>
    internal static List<InvoiceFile> Expand(IEnumerable<string> paths, bool recursive)
    {
        var search = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = new List<InvoiceFile>();

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                files.AddRange(Directory
                    .EnumerateFiles(path, "*", search)
                    .Where(file => Extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                    .Select(file => new InvoiceFile(file, Path.GetRelativePath(path, file))));
            }
            else
            {
                files.Add(new InvoiceFile(path, Path.GetFileName(path)));
            }
        }

        return files
            .DistinctBy(file => file.FullPath, StringComparer.OrdinalIgnoreCase)
            .OrderBy(file => file.FullPath, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Validates every file, returning what completed. Cancelling stops new files being started;
    /// whatever finished is still reported, because a partial answer over an archive is worth more
    /// than none.
    /// </summary>
    internal static List<Report> Run(
        InvoiceValidator validator,
        List<InvoiceFile> files,
        RuleSet? ruleSet,
        bool validateSchema,
        bool strict = false,
        int? maxConcurrency = null,
        CancellationToken cancellationToken = default)
    {
        var reports = new ConcurrentBag<Report>();

        // The validator caches compiled stylesheets and is thread-safe, so a large archive is
        // worth spreading across cores; a single file is not. Bounded on purpose: each worker holds
        // a parsed document and a Saxon transformer, so unbounded parallelism buys nothing and
        // costs memory a host cannot predict.
        if (files.Count > 1)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency ?? Environment.ProcessorCount,
                CancellationToken = cancellationToken,
            };

            try
            {
                Parallel.ForEach(files, options, file =>
                    reports.Add(Validate(validator, file.FullPath, ruleSet, validateSchema, strict, cancellationToken)));
            }
            catch (OperationCanceledException)
            {
                // Keep what finished. The caller reports the run as incomplete from the token, so a
                // cancelled run cannot be mistaken for a clean archive.
            }
            catch (AggregateException exception)
            {
                // Parallel.ForEach wraps whatever a worker threw. Validate keeps a file's own
                // problems as a row, so what escapes is a problem with the run — a missing or
                // altered artefact — and wrapped, it reached no handler: exit 127 and a stack trace
                // over two files where one file gave exit 2 and a sentence. Rethrown as itself.
                ExceptionDispatchInfo.Throw(exception.Flatten().InnerExceptions.FirstOrDefault() ?? exception);
            }
        }
        else
        {
            try
            {
                reports.Add(Validate(validator, files[0].FullPath, ruleSet, validateSchema, strict, cancellationToken));
            }
            catch (OperationCanceledException)
            {
            }
        }

        return reports.OrderBy(report => report.File, StringComparer.Ordinal).ToList();
    }

    private static Report Validate(
        InvoiceValidator validator,
        string file,
        RuleSet? ruleSet,
        bool validateSchema,
        bool strict,
        CancellationToken cancellationToken)
    {
        try
        {
            var document = InvoiceDocument.Load(file);
            var result = validator.Validate(document, ruleSet, validateSchema, strict, cancellationToken);

            return new Report(
                file,
                result.IsValid ? "valid" : "invalid",
                result.RuleSet.ToString(),
                result.SchemaChecked,
                result.SchemaValid,
                result.ProfileCovered,
                document.Profile.SpecificationIdentifier,
                document.EmbeddedFileName,
                result.Findings.Select(Finding.From).ToList(),
                null);
        }
        // Reported as a row rather than thrown: one unreadable file in an archive of thousands
        // should not abandon the run, and Parallel.ForEach would surface it as an AggregateException.
        // Malformed XML arrives as UnsupportedDocumentException, wrapped by InvoiceDocument.
        catch (Exception exception) when (exception is UnsupportedDocumentException or ValidationException
                                             or IOException or UnauthorizedAccessException)
        {
            return new Report(file, "error", null, false, false, false, null, null, [], exception.Message);
        }
    }

    internal static void WriteCsv(TextWriter writer, IEnumerable<Report> reports)
    {
        // schemaChecked and profileCovered are what turn "valid" into a statement you can act on:
        // valid against what, and was the schema even looked at.
        writer.WriteLine(
            "file,status,ruleSet,profile,profileCovered,schemaChecked,schemaValid,errors,warnings,rules,detail");

        foreach (var report in reports)
        {
            var errors = report.Findings.Count(f => f.Severity == nameof(ValidationSeverity.Error));
            var warnings = report.Findings.Count(f => f.Severity == nameof(ValidationSeverity.Warning));
            var rules = string.Join(";", report.Findings
                .Where(f => f.Severity == nameof(ValidationSeverity.Error))
                .Select(f => f.RuleId)
                .Distinct());

            writer.WriteLine(string.Join(',', [
                Escape(report.File),
                report.Status,
                report.RuleSet ?? string.Empty,
                Escape(report.Profile ?? string.Empty),
                report.ProfileCovered.ToString(CultureInfo.InvariantCulture),
                report.SchemaChecked.ToString(CultureInfo.InvariantCulture),
                report.SchemaValid.ToString(CultureInfo.InvariantCulture),
                errors.ToString(CultureInfo.InvariantCulture),
                warnings.ToString(CultureInfo.InvariantCulture),
                Escape(rules),
                Escape(report.Error ?? string.Empty),
            ]));
        }
    }

    /// <summary>The part an ISV actually reads: how much of the archive would be rejected, and why.</summary>
    internal static void WriteSummary(TextWriter writer, IReadOnlyList<Report> reports, int requested = 0)
    {
        if (requested > reports.Count)
        {
            writer.WriteLine();
            writer.WriteLine($"Stopped early: {reports.Count} of {requested} files were checked. " +
                             "The remainder were not looked at.");
        }

        var valid = reports.Count(report => report.Status == "valid");
        var invalid = reports.Count(report => report.Status == "invalid");
        var unreadable = reports.Count(report => report.Status == "error");
        var schemaFailures = reports.Count(report => report.Status != "error" && !report.SchemaValid);

        writer.WriteLine();
        writer.WriteLine($"{reports.Count} file(s): {valid} valid, {invalid} with errors, {unreadable} unreadable");

        if (schemaFailures > 0)
        {
            writer.WriteLine($"{schemaFailures} did not conform to their XML Schema, so business rules were not run.");
        }

        var uncovered = reports.Count(report => report.Status != "error" && !report.ProfileCovered);
        if (uncovered > 0)
        {
            writer.WriteLine($"{uncovered} declared a specification with no rule set here and were judged " +
                             "against EN 16931 alone. Use --strict to treat that as a failure.");
        }

        if (reports.Any(report => report.Status != "error" && !report.SchemaChecked))
        {
            writer.WriteLine("XML Schema was not checked, so \"valid\" covers the business rules only.");
        }

        var byRule = reports
            .SelectMany(report => report.Findings
                .Where(finding => finding.Severity == nameof(ValidationSeverity.Error))
                .Select(finding => finding.RuleId)
                .Distinct())
            .GroupBy(ruleId => ruleId)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Take(10)
            .ToList();

        if (byRule.Count == 0) return;

        writer.WriteLine();
        writer.WriteLine("Most frequent errors:");
        foreach (var group in byRule)
        {
            writer.WriteLine($"  {group.Key,-24} {group.Count(),5} file(s)");
        }
    }

    private static readonly char[] Delimiters = [',', '"', '\n', '\r', '\t'];

    /// <summary>
    /// One CSV field: quoted when it carries a delimiter, and made inert when it opens with a
    /// character a spreadsheet reads as an expression rather than text.
    /// </summary>
    /// <remarks>
    /// The profile column is the invoice's own cbc:CustomizationID, and the file column can come
    /// from an archive somebody else named, so both arrive exactly as a sender wrote them. Excel
    /// and LibreOffice evaluate a field beginning = + - or @, which turns opening the report into
    /// running what the invoice asked for; quoting does not stop it, because the quotes are gone by
    /// the time the field is parsed. A leading tab does, and reads as whitespace everywhere else.
    /// The --json output carries the value untouched for anything reading it programmatically.
    /// </remarks>
    private static string Escape(string value)
    {
        if (value.Length == 0) return value;

        var field = value[0] is '=' or '+' or '-' or '@' ? '\t' + value : value;

        if (field.IndexOfAny(Delimiters) < 0) return field;

        return '"' + field.Replace("\"", "\"\"") + '"';
    }
}

/// <summary>An invoice to process, and where it sat relative to the folder it was found in.</summary>
internal sealed record InvoiceFile(string FullPath, string RelativePath);

internal sealed record Report(
    string File,
    string Status,
    string? RuleSet,
    bool SchemaChecked,
    bool SchemaValid,
    bool ProfileCovered,
    string? Profile,
    string? Attachment,
    List<Finding> Findings,
    string? Error);

internal sealed record Finding(
    string RuleId,
    string Severity,
    string Message,
    string Location,
    string? Value,
    List<string> BusinessTerms,
    string Artefact)
{
    internal static Finding From(ValidationFinding finding) => new(
        finding.RuleId,
        finding.Severity.ToString(),
        finding.Message,
        finding.Location,
        finding.Value,
        finding.BusinessTerms.ToList(),
        finding.Artefact);
}
