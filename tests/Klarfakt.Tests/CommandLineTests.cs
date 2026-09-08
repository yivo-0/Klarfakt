namespace Klarfakt.Tests;

/// <summary>
/// Argument parsing had no tests, because it lived in top-level statements where nothing could
/// reach it — and it kept every option it did not recognise. A mistyped --stict went into the flag
/// set, nothing read it, and the run reported exit 0 and "valid" for an archive nobody had
/// validated strictly. That is the answer a pipeline goes on to trust.
/// </summary>
public class CommandLineTests
{
    [Theory]
    [InlineData("--stict", "--strict")]
    [InlineData("--recursve", "--recursive")]
    [InlineData("--jsonn", "--json")]
    [InlineData("--rule", "--rules")]
    public void Refuses_an_option_it_does_not_know_and_names_the_nearest(string typo, string meant)
    {
        var parsed = CommandLine.Parse(["corpus", typo]);

        Assert.NotNull(parsed.Problem);
        Assert.Contains($"Unknown option '{typo}'", parsed.Problem);
        Assert.Contains($"Did you mean '{meant}'", parsed.Problem);
    }

    [Fact]
    public void Refuses_an_option_far_from_anything_without_guessing()
    {
        var parsed = CommandLine.Parse(["--wildly-different"]);

        Assert.Contains("Unknown option '--wildly-different'", parsed.Problem);
        Assert.DoesNotContain("Did you mean", parsed.Problem);
    }

    [Fact]
    public void Refuses_an_unknown_option_written_with_an_equals_sign()
    {
        // --stict=1 took a different branch and was kept as a value nobody read.
        var parsed = CommandLine.Parse(["--stict=1"]);

        Assert.Contains("Unknown option '--stict'", parsed.Problem);
    }

    [Fact]
    public void Refuses_a_value_option_with_nothing_after_it()
    {
        // Trailing --rules used to fall through to the flag set, so the rule set silently defaulted
        // to the one taken from the invoice rather than the one that was asked for.
        var parsed = CommandLine.Parse(["corpus", "--rules"]);

        Assert.Contains("'--rules' needs a value", parsed.Problem);
    }

    [Fact]
    public void Takes_a_value_option_in_either_form()
    {
        Assert.Equal("xrechnung", CommandLine.Parse(["--rules", "xrechnung"]).Values["--rules"]);
        Assert.Equal("xrechnung", CommandLine.Parse(["--rules=xrechnung"]).Values["--rules"]);
    }

    [Fact]
    public void Does_not_read_a_value_option_argument_as_a_path()
    {
        var parsed = CommandLine.Parse(["--rules", "xrechnung", "corpus"]);

        Assert.Null(parsed.Problem);
        Assert.Equal(["corpus"], parsed.Paths);
    }

    [Fact]
    public void Keeps_paths_flags_and_values_apart()
    {
        var parsed = CommandLine.Parse(["a.xml", "b.pdf", "--recursive", "--strict", "--parallel", "4"]);

        Assert.Null(parsed.Problem);
        Assert.Equal(["a.xml", "b.pdf"], parsed.Paths);
        Assert.Equal(["--recursive", "--strict"], parsed.Flags.Order());
        Assert.Equal("4", parsed.Values["--parallel"]);
    }

    [Fact]
    public void Accepts_every_option_the_usage_text_documents()
    {
        // The known set and the usage text are two lists of the same thing, which is how they drift.
        var usage = new StringWriter();
        var original = Console.Out;

        try
        {
            Console.SetOut(usage);
            typeof(ExitCode).Assembly.EntryPoint!.Invoke(null, [new[] { "--help" }]);
        }
        finally
        {
            Console.SetOut(original);
        }

        var documented = System.Text.RegularExpressions.Regex
            .Matches(usage.ToString(), @"(?<![\w-])--[a-z][a-z-]*")
            .Select(match => match.Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(documented);

        var known = CommandLine.FlagOptions.Concat(CommandLine.ValueOptions).ToHashSet(StringComparer.Ordinal);
        var undocumented = documented.Where(option => !known.Contains(option)).ToList();

        Assert.True(undocumented.Count == 0,
            $"the usage text documents options the parser will refuse: {string.Join(", ", undocumented)}");
    }
}
