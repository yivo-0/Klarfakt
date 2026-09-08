using System.Text.RegularExpressions;

namespace Klarfakt.Tests;

/// <summary>
/// Argument parsing had no tests, because it lived in top-level statements where nothing could
/// reach it — and it kept everything it did not recognise. A mistyped --stict went into the flag
/// set and the run reported exit 0 for an archive nobody had validated strictly; then a global
/// option set fixed that and left the same hole one level up, where "info --csv report.csv"
/// parsed, wrote nothing, said nothing and exited 0 because info never reads --csv.
/// </summary>
public class CommandLineTests
{
    [Theory]
    [InlineData("--stict", "--strict")]
    [InlineData("--recursve", "--recursive")]
    [InlineData("--jsonn", "--json")]
    public void Refuses_an_option_it_does_not_know_and_names_the_nearest(string typo, string meant)
    {
        var parsed = CommandLine.Parse("validate", ["corpus", typo]);

        Assert.NotNull(parsed.Problem);
        Assert.Contains($"Unknown option '{typo}'", parsed.Problem);
        Assert.Contains($"Did you mean '{meant}'", parsed.Problem);
    }

    [Fact]
    public void Refuses_an_option_far_from_anything_without_guessing()
    {
        var parsed = CommandLine.Parse("validate", ["--wildly-different"]);

        Assert.Contains("Unknown option '--wildly-different'", parsed.Problem);
        Assert.DoesNotContain("Did you mean", parsed.Problem);
    }

    [Theory]
    [InlineData("info", "--csv", "validate")]
    [InlineData("info", "--strict", "validate")]
    [InlineData("render", "--strict", "validate")]
    [InlineData("render", "--json", "info, validate")]
    [InlineData("validate", "--lang", "render")]
    [InlineData("validate", "-o", "render")]
    [InlineData("rules", "--recursive", "info, render, validate")]
    public void Refuses_a_real_option_aimed_at_a_command_that_does_not_read_it(
        string command, string option, string elsewhere)
    {
        // Every one of these parsed cleanly and was dropped. "info --csv report.csv" is the one a
        // pipeline would trust: exit 0, no file, no message.
        var parsed = CommandLine.Parse(command, ["corpus", option, "value"]);

        Assert.Equal($"'{option}' is not an option for '{command}'. It applies to: {elsewhere}.", parsed.Problem);
    }

    [Fact]
    public void Refuses_an_unknown_option_written_with_an_equals_sign()
    {
        Assert.Contains("Unknown option '--stict'", CommandLine.Parse("validate", ["--stict=1"]).Problem);
        Assert.Contains("is not an option for 'info'", CommandLine.Parse("info", ["--csv=x"]).Problem);
    }

    [Fact]
    public void Refuses_a_value_option_with_nothing_after_it()
    {
        var parsed = CommandLine.Parse("validate", ["corpus", "--rules"]);

        Assert.Contains("'--rules' needs a value", parsed.Problem);
    }

    [Fact]
    public void Takes_help_on_any_command()
    {
        foreach (var command in CommandLine.ByCommand.Keys)
        {
            var parsed = CommandLine.Parse(command, ["--help"]);

            Assert.Null(parsed.Problem);
            Assert.True(parsed.WantsHelp);
        }
    }

    [Fact]
    public void Takes_a_value_option_in_either_form()
    {
        Assert.Equal("xrechnung", CommandLine.Parse("validate", ["--rules", "xrechnung"]).Values["--rules"]);
        Assert.Equal("xrechnung", CommandLine.Parse("validate", ["--rules=xrechnung"]).Values["--rules"]);
    }

    [Fact]
    public void Does_not_read_a_value_option_argument_as_a_path()
    {
        var parsed = CommandLine.Parse("validate", ["--rules", "xrechnung", "corpus"]);

        Assert.Null(parsed.Problem);
        Assert.Equal(["corpus"], parsed.Paths);
    }

    [Fact]
    public void Keeps_paths_flags_and_values_apart()
    {
        var parsed = CommandLine.Parse(
            "validate", ["a.xml", "b.pdf", "--recursive", "--strict", "--parallel", "4"]);

        Assert.Null(parsed.Problem);
        Assert.Equal(["a.xml", "b.pdf"], parsed.Paths);
        Assert.Equal(["--recursive", "--strict"], parsed.Flags.Order());
        Assert.Equal("4", parsed.Values["--parallel"]);
    }

    [Fact]
    public void Accepts_every_option_the_usage_text_documents()
    {
        // The known set and the usage text are two lists of the same thing, which is how they drift.
        var documented = Regex.Matches(CommandLine.UsageText, @"(?<![\w-])--[a-z][a-z-]*")
            .Select(match => match.Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(documented);

        var known = CommandLine.FlagOptions.Concat(CommandLine.ValueOptions).ToHashSet(StringComparer.Ordinal);
        var undocumented = documented.Where(option => !known.Contains(option)).ToList();

        Assert.True(undocumented.Count == 0,
            $"the usage text documents options the parser will refuse: {string.Join(", ", undocumented)}");
    }

    [Fact]
    public void Every_command_accepts_exactly_the_options_it_reads()
    {
        // The whole point of the per-command sets is that they match what Program.cs actually
        // looks at. They are the same list written twice, so this holds one against the other.
        var source = ProgramSource();
        if (source is null) return;

        foreach (var (command, declared) in CommandLine.ByCommand)
        {
            var read = OptionsReadBy(source, command);

            Assert.True(read.Count > 0, $"found no option reads for '{command}'; the scan is broken, not the code");

            Assert.Equal(declared.Order(StringComparer.Ordinal), read.Order(StringComparer.Ordinal));
        }
    }

    /// <summary>
    /// The option literals Program.cs reads for one command: the arguments passed in its switch
    /// arm, plus the bodies of every top-level function reachable from it. Following calls matters
    /// — Validate() reads --rules and --parallel through RuleSet() and Parallelism().
    /// </summary>
    private static HashSet<string> OptionsReadBy(string source, string command)
    {
        // To the end of the line, not to the first ")": rules reads its only option in the arm
        // itself — await Rules(paths.FirstOrDefault(), flags.Contains("--force")).
        var arm = Regex.Match(
            source, $@"""{command}""\s*=>\s*(?:await\s+)?(\w+)\((.*)$", RegexOptions.Multiline);

        if (!arm.Success) return [];

        var functions = TopLevelFunctions(source);
        var text = new System.Text.StringBuilder(arm.Groups[2].Value);
        var pending = new Stack<string>([arm.Groups[1].Value]);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        while (pending.Count > 0)
        {
            var name = pending.Pop();
            if (!seen.Add(name) || !functions.TryGetValue(name, out var body)) continue;

            text.Append(body);

            foreach (Match call in Regex.Matches(body, @"\b(\w+)\("))
            {
                if (functions.ContainsKey(call.Groups[1].Value)) pending.Push(call.Groups[1].Value);
            }
        }

        return Regex.Matches(text.ToString(), @"(?:Contains|TryGetValue)\(""(-{1,2}[a-z][a-z-]*)""")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Every top-level local function, keyed by name. They sit at column zero and follow one
    /// another, so each runs from its own signature to the next.
    /// </summary>
    private static Dictionary<string, string> TopLevelFunctions(string source)
    {
        var signature = new Regex(
            @"^(?:static\s+|async\s+)*[\w<>?.\[\]]+\s+(\w+)\([^)]*\)\s*(?:=>|\r?\n?\{)",
            RegexOptions.Multiline);

        var matches = signature.Matches(source).ToList();
        var functions = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 0; index < matches.Count; index++)
        {
            var start = matches[index].Index;
            var end = index + 1 < matches.Count ? matches[index + 1].Index : source.Length;
            functions[matches[index].Groups[1].Value] = source[start..end];
        }

        return functions;
    }

    /// <summary>The CLI source, when the tests are run from a checkout. Null from a bare package.</summary>
    private static string? ProgramSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Klarfakt.Cli", "Program.cs");
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            directory = directory.Parent;
        }

        return null;
    }
}
