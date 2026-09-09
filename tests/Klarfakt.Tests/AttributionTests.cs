using System.Text.Json;
using System.Text.RegularExpressions;

namespace Klarfakt.Tests;

/// <summary>
/// Version numbers written down in prose drift from the ones that ship. NOTICE named AngleSharp
/// 1.7.3 while the project pinned 1.8.0, because Dependabot bumped the pin and prose is not
/// something Dependabot edits — and the transitive pins exist to keep being bumped, so it would
/// have recurred. The rule pack versions are worse: correct today, and repeated across five files.
/// </summary>
public class AttributionTests
{
    [Fact]
    public void NOTICE_names_the_dependency_versions_that_ship()
    {
        var root = Repository();
        if (root is null) return;

        var notice = File.ReadAllText(Path.Combine(root, "NOTICE"));
        var project = File.ReadAllText(Path.Combine(root, "src", "Klarfakt", "Klarfakt.csproj"));

        var references = Regex.Matches(project, @"<PackageReference Include=""([^""]+)"" Version=""([^""]+)""")
            .Select(match => (Package: match.Groups[1].Value, Version: match.Groups[2].Value))
            .ToList();

        Assert.NotEmpty(references);

        var wrong = references
            .Where(reference => notice.Contains(reference.Package, StringComparison.Ordinal))
            .Where(reference => !notice.Contains($"{reference.Package} {reference.Version}", StringComparison.Ordinal))
            .Select(reference => $"{reference.Package} {reference.Version}")
            .ToList();

        Assert.True(wrong.Count == 0,
            "NOTICE ships inside the package and names a version the package does not contain: "
            + string.Join(", ", wrong));
    }

    [Fact]
    public void Every_rule_pack_version_in_the_documentation_is_one_that_ships()
    {
        var root = Repository();
        if (root is null) return;

        var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "src", "Klarfakt", "RulePacks.json")));

        var shipped = manifest.RootElement.GetProperty("packs")
            .EnumerateArray()
            .Select(pack => (Id: pack.GetProperty("id").GetString()!, Version: pack.GetProperty("version").GetString()!))
            .ToList();

        Assert.NotEmpty(shipped);

        // Where a document names a pack and a version on the same line, the version has to be the
        // one shipped. Bumping a pack means editing every one of these, and there are twenty.
        var stale = new List<string>();

        foreach (var document in new[]
        {
            "README.md", "package-readme.md", "CHANGELOG.md",
            Path.Combine("docs", "index.md"), Path.Combine("docs", "en16931-2026.md"),
        })
        {
            var path = Path.Combine(root, document);
            if (!File.Exists(path)) continue;

            var lines = File.ReadAllLines(path);

            for (var number = 0; number < lines.Length; number++)
            {
                foreach (var (id, version) in shipped)
                {
                    if (!lines[number].Contains(id, StringComparison.Ordinal)) continue;

                    // A version-shaped token on the same line as a pack id, that is not this pack's
                    // version and not another pack's, has gone stale.
                    var others = shipped.Select(pack => pack.Version).ToHashSet(StringComparer.Ordinal);

                    foreach (var candidate in Regex.Matches(lines[number], @"\b\d+\.\d+\.\d+\b|\b\d{4}-\d{2}-\d{2}\b")
                                 .Select(match => match.Value)
                                 .Where(value => !others.Contains(value)))
                    {
                        stale.Add($"{document}:{number + 1} names `{id}` alongside {candidate}, " +
                                  $"but the shipped version is {version}");
                    }
                }
            }
        }

        Assert.True(stale.Count == 0, string.Join("\n", stale.Distinct()));
    }

    /// <summary>The repository root, when the tests run from a checkout. Null from a bare package.</summary>
    private static string? Repository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NOTICE"))) return directory.FullName;
            directory = directory.Parent;
        }

        return null;
    }
}
