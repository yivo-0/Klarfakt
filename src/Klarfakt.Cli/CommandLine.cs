/// <summary>
/// The arguments after the command name.
/// </summary>
/// <remarks>
/// Every option is named here, per command, and anything a command does not read is refused. A
/// global set was not enough: "klarfakt info ./invoices --csv report.csv" parsed cleanly, wrote no
/// CSV, said nothing and exited 0, because info never looks at --csv. That is the same silent
/// ignore one level up from the one this class was written to remove.
/// </remarks>
internal sealed class CommandLine
{
    /// <summary>Options that take the next argument as their value.</summary>
    internal static readonly string[] ValueOptions =
        ["--rules", "--csv", "--out", "-o", "--lang", "--parallel"];

    /// <summary>Options that stand alone.</summary>
    internal static readonly string[] FlagOptions =
        ["--recursive", "-r", "--json", "--no-schema", "--strict", "--force", "--help", "-h"];

    /// <summary>Accepted anywhere, because asking for help is not specific to a command.</summary>
    internal static readonly string[] Universal = ["--help", "-h"];

    /// <summary>
    /// What each command actually reads. Kept as data so a test can hold it against the option
    /// literals in Program.cs — the two are the same list written twice, which is how they drift.
    /// </summary>
    internal static readonly Dictionary<string, string[]> ByCommand = new(StringComparer.Ordinal)
    {
        ["rules"] = ["--force"],
        ["validate"] = ["--recursive", "-r", "--no-schema", "--strict", "--json", "--csv", "--parallel", "--rules"],
        ["render"] = ["--recursive", "-r", "--lang", "--out", "-o"],
        ["info"] = ["--recursive", "-r", "--json"],
    };

    private CommandLine(List<string> paths, HashSet<string> flags, Dictionary<string, string> values, string? problem)
    {
        Paths = paths;
        Flags = flags;
        Values = values;
        Problem = problem;
    }

    internal List<string> Paths { get; }

    internal HashSet<string> Flags { get; }

    internal Dictionary<string, string> Values { get; }

    /// <summary>What was wrong with the arguments, or null when they parsed.</summary>
    internal string? Problem { get; }

    internal bool WantsHelp => Flags.Contains("--help") || Flags.Contains("-h");

    internal static bool IsCommand(string command) => ByCommand.ContainsKey(command);

    internal static CommandLine Parse(string command, IReadOnlyList<string> arguments)
    {
        var accepted = ByCommand.TryGetValue(command, out var own)
            ? own.Concat(Universal).ToHashSet(StringComparer.Ordinal)
            : FlagOptions.Concat(ValueOptions).ToHashSet(StringComparer.Ordinal);

        var paths = new List<string>();
        var flags = new HashSet<string>(StringComparer.Ordinal);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];

            if (!argument.StartsWith('-'))
            {
                paths.Add(argument);
                continue;
            }

            // --rules=xrechnung as well as --rules xrechnung: both forms turn up in scripts.
            var equals = argument.IndexOf('=');
            var name = equals > 0 ? argument[..equals] : argument;

            if (!accepted.Contains(name))
            {
                return Failed(paths, flags, values, Rejected(command, name));
            }

            if (equals > 0)
            {
                values[name] = argument[(equals + 1)..];
                continue;
            }

            // Consumed with their argument, otherwise "--rules xrechnung" would treat "xrechnung"
            // as a file to validate.
            if (ValueOptions.Contains(name, StringComparer.Ordinal))
            {
                if (index + 1 >= arguments.Count)
                {
                    return Failed(paths, flags, values, $"'{name}' needs a value.");
                }

                values[name] = arguments[++index];
                continue;
            }

            flags.Add(name);
        }

        return new CommandLine(paths, flags, values, Rejects(values));
    }

    /// <summary>
    /// An option whose value makes no sense is a usage error like any other. These used to be
    /// checked where they were read and thrown as RulePackException, which the CLI maps to exit 2
    /// — "a file could not be processed" — for what is a typo in the command line.
    /// </summary>
    private static string? Rejects(Dictionary<string, string> values)
    {
        if (values.TryGetValue("--rules", out var rules) && RuleSetNames.All(
                name => !string.Equals(name, rules, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Unknown rule set '{rules}'. Use {string.Join(", ", RuleSetNames)}.";
        }

        if (values.TryGetValue("--parallel", out var parallel)
            && !(int.TryParse(parallel, out var count) && count > 0))
        {
            return $"'--parallel' needs a positive number, not '{parallel}'.";
        }

        // The third value option, and the one the sweep that added the other two missed. --lang
        // reached the KoSIT stylesheet untouched, where it names a decimal-format that only exists
        // for de and en — so "--lang fr" produced nine lines of Saxon internals, complete with
        // local file paths, and exit 2 for a typo. Over a folder, once per invoice.
        if (values.TryGetValue("--lang", out var language) && Klarfakt.Rendering.InvoiceRenderer.Languages.All(
                known => !string.Equals(known, language, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Unknown language '{language}'. Use " +
                   $"{string.Join(", ", Klarfakt.Rendering.InvoiceRenderer.Languages)}.";
        }

        return null;
    }

    internal static readonly string[] RuleSetNames = ["en16931", "peppol", "xrechnung"];

    /// <summary>
    /// A real option aimed at the wrong command is a different mistake from a typo, and worth a
    /// different sentence: the first is answered by naming the commands that take it.
    /// </summary>
    private static string Rejected(string command, string option)
    {
        var elsewhere = ByCommand
            .Where(entry => entry.Value.Contains(option, StringComparer.Ordinal))
            .Select(entry => entry.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (elsewhere.Count > 0)
        {
            return $"'{option}' is not an option for '{command}'. It applies to: {string.Join(", ", elsewhere)}.";
        }

        var suggestion = FlagOptions.Concat(ValueOptions)
            .Where(known => Distance(known, option) <= 2)
            .OrderBy(known => Distance(known, option))
            .FirstOrDefault();

        return $"Unknown option '{option}'."
               + (suggestion is null ? string.Empty : $" Did you mean '{suggestion}'?");
    }

    private static CommandLine Failed(
        List<string> paths, HashSet<string> flags, Dictionary<string, string> values, string problem) =>
        new(paths, flags, values, problem);

    /// <summary>Levenshtein distance, only ever run once on a handful of short strings.</summary>
    private static int Distance(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var column = 0; column <= right.Length; column++) previous[column] = column;

        for (var row = 1; row <= left.Length; row++)
        {
            current[0] = row;

            for (var column = 1; column <= right.Length; column++)
            {
                var substitution = previous[column - 1] + (left[row - 1] == right[column - 1] ? 0 : 1);
                current[column] = Math.Min(Math.Min(current[column - 1] + 1, previous[column] + 1), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    /// <summary>
    /// Held here rather than written straight to the console so a test can read it without
    /// redirecting process-global stdout underneath a parallel test runner.
    /// </summary>
    internal const string UsageText =
        """
        klarfakt — validate EN 16931 electronic invoices

        Usage:
          klarfakt rules restore [--force]
          klarfakt rules verify
          klarfakt validate <file|folder...> [options]
          klarfakt render <file|folder...> [-o <file|folder>] [--lang de|en]
          klarfakt info <file|folder...> [--json]

        Options:
          --rules <set>   en16931 | peppol | xrechnung (default: taken from the invoice)
          --recursive     descend into subfolders, -r for short
          --csv <file>    write one row per invoice, with a summary on the console
          --json          machine-readable output
          --no-schema     check business rules only, skipping XML Schema
          --strict        fail an invoice whose declared specification has no rule set
                          here, instead of judging it against EN 16931 alone
          --parallel <n>  files validated at once (default: one per core)
          -o, --out       where to write rendered HTML; stdout for a single invoice
          --lang          label language for render: de (default) or en

        Options apply to the command that reads them; anything else is refused rather
        than ignored. "klarfakt <command> --help" lists what a command takes.

        render produces a self-contained HTML page using the official XRechnung
        visualisation, so what you show a user matches the reference rendering.

        Run "rules restore" once: it downloads the validation artefacts from their
        publishers and checks them against the SHA-256 recorded in the manifest.
        Nothing else in Klarfakt touches the network.

        Files may be UBL or CII XML, or a hybrid Factur-X / ZUGFeRD 2.x PDF. Point it at a
        folder to see how much of an existing archive would be rejected.

        Exit codes:
          0   valid
          1   validation errors
          2   a file could not be processed
          64  usage error
          130 stopped with Ctrl+C before every file was checked
        """;
}
