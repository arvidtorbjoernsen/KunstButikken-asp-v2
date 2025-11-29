using System.Collections.Concurrent;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

MSBuildLocator.RegisterDefaults();

var repoRoot = args.FirstOrDefault() ?? Directory.GetCurrentDirectory();
var solutionPath = Path.Combine(repoRoot, "KunstButikken-asp.sln");

if (!File.Exists(solutionPath))
{
    Console.Error.WriteLine($"Solution not found at {solutionPath}");
    return;
}

using var workspace = MSBuildWorkspace.Create();
var solution = await workspace.OpenSolutionAsync(solutionPath);

var centralVersions = LoadCentralPackageVersions(repoRoot);
var projectPackageMap = await LoadPackageReferencesAsync(repoRoot, centralVersions);
var frameworkFilter = LoadFrameworkFilter(repoRoot);

var report = new List<ProjectReport>();
var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
var errors = new ConcurrentBag<string>();

await Parallel.ForEachAsync(solution.Projects, parallelOptions, async (project, token) =>
{
    if (!project.FilePath?.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ?? true)
    {
        return;
    }

    var declaredPackages = projectPackageMap.TryGetValue(project.FilePath!, out var declared)
        ? declared
        : new List<PackageInfo>();
    var declaredIds = new HashSet<string>(declaredPackages.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);

    var usedPackages = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    try
    {
        var compilation = await project.GetCompilationAsync(token);
        if (compilation is null)
        {
            errors.Add($"Unable to compile {project.Name}");
            return;
        }

        foreach (var reference in compilation.ExternalReferences)
        {
            var display = reference.Display;
            if (string.IsNullOrWhiteSpace(display))
            {
                continue;
            }

            var usage = InferPackageUsage(display);
            if (usage is null)
            {
                continue;
            }

            usedPackages[usage.Id] = usage.Version ?? usedPackages.GetValueOrDefault(usage.Id);
        }
    }
    catch (Exception ex)
    {
        errors.Add($"Failed to analyze {project.Name}: {ex.Message}");
        return;
    }

    var directUsed = declaredPackages
        .Where(p => usedPackages.ContainsKey(p.Id))
        .Select(p => new PackageInfo(p.Id, p.Version))
        .ToList();

    var transitiveUsed = usedPackages
        .Where(kvp => !declaredIds.Contains(kvp.Key) && !frameworkFilter.ShouldIgnore(kvp.Key))
        .Select(kvp => new PackageInfo(kvp.Key, kvp.Value ?? centralVersions.GetValueOrDefault(kvp.Key) ?? "unknown"))
        .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
        .ToList();

    var missingDirectReferences = transitiveUsed
        .Where(p => centralVersions.ContainsKey(p.Id))
        .ToList();

    lock (report)
    {
        report.Add(new ProjectReport
        {
            ProjectName = project.Name,
            ProjectPath = project.FilePath!,
            DeclaredPackages = declaredPackages,
            DirectUsedPackages = directUsed,
            TransitiveUsedPackages = transitiveUsed,
            MissingDirectPackages = missingDirectReferences
        });
    }
});

var summaryPath = Path.Combine(repoRoot, "package-usage-report.md");
await using (var writer = new StreamWriter(summaryPath))
{
    foreach (var entry in report.OrderBy(r => r.ProjectName, StringComparer.OrdinalIgnoreCase))
    {
        await writer.WriteLineAsync($"Project name: {entry.ProjectName}");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("Used (direct):");
        if (entry.DirectUsedPackages.Count == 0)
        {
            await writer.WriteLineAsync("- None");
        }
        else
        {
            foreach (var pkg in entry.DirectUsedPackages.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync($"- {pkg.Id} ({pkg.Version})");
            }
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("Used (transitive):");
        if (entry.TransitiveUsedPackages.Count == 0)
        {
            await writer.WriteLineAsync("- None");
        }
        else
        {
            foreach (var pkg in entry.TransitiveUsedPackages)
            {
                await writer.WriteLineAsync($"- {pkg.Id} ({pkg.Version})");
            }
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("In csproj:");
        if (entry.DeclaredPackages.Count == 0)
        {
            await writer.WriteLineAsync("- None");
        }
        else
        {
            foreach (var pkg in entry.DeclaredPackages.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync($"- {pkg.Id} ({pkg.Version})");
            }
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("Should add to csproj:");
        if (entry.MissingDirectPackages.Count == 0)
        {
            await writer.WriteLineAsync("- None");
        }
        else
        {
            foreach (var pkg in entry.MissingDirectPackages)
            {
                await writer.WriteLineAsync($"- {pkg.Id} ({pkg.Version})");
            }
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync();
    }

    if (!errors.IsEmpty)
    {
        await writer.WriteLineAsync("Errors:");
        foreach (var err in errors)
        {
            await writer.WriteLineAsync($"- {err}");
        }
    }
}

Console.WriteLine($"Report written to {summaryPath}");

static PackageUsage? InferPackageUsage(string referenceDisplay)
{
    var segments = referenceDisplay.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
    for (var i = 0; i < segments.Length - 3; i++)
    {
        if (segments[i + 2].Equals("lib", StringComparison.OrdinalIgnoreCase))
        {
            var id = segments[i];
            var version = segments[i + 1];
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(version))
            {
                return new PackageUsage(id, version);
            }
        }
    }

    return null;
}

static async Task<Dictionary<string, List<PackageInfo>>> LoadPackageReferencesAsync(string root, Dictionary<string, string> centralVersions)
{
    var result = new Dictionary<string, List<PackageInfo>>(StringComparer.OrdinalIgnoreCase);
    foreach (var csproj in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
    {
        var doc = XDocument.Load(csproj);
        var packages = new List<PackageInfo>();
        foreach (var pkg in doc.Descendants().Where(e => e.Name.LocalName == "PackageReference"))
        {
            var id = pkg.Attribute("Include")?.Value;
            var version = pkg.Attribute("Version")?.Value ?? centralVersions.GetValueOrDefault(id ?? string.Empty);
            if (!string.IsNullOrEmpty(id))
            {
                packages.Add(new PackageInfo(id, version ?? "unknown"));
            }
        }

        result[csproj] = packages;
    }

    return result;
}

static Dictionary<string, string> LoadCentralPackageVersions(string root)
{
    var propsPath = Path.Combine(root, "Directory.Packages.props");
    var centralVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    if (File.Exists(propsPath))
    {
        var doc = XDocument.Load(propsPath);
        foreach (var pkg in doc.Descendants().Where(e => e.Name.LocalName == "PackageVersion"))
        {
            var id = pkg.Attribute("Include")?.Value;
            var version = pkg.Attribute("Version")?.Value;
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
            {
                centralVersions[id] = version;
            }
        }
    }

    return centralVersions;
}

static FrameworkFilter LoadFrameworkFilter(string root)
{
    var filterPath = Path.Combine(root, "package-usage-filters.json");
    if (!File.Exists(filterPath))
    {
        return FrameworkFilter.Empty;
    }

    try
    {
        using var stream = File.OpenRead(filterPath);
        var config = JsonSerializer.Deserialize<FrameworkFilterConfig>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new FrameworkFilterConfig();

        return new FrameworkFilter(config.IgnorePackages, config.IgnorePrefixes);
    }
    catch
    {
        return FrameworkFilter.Empty;
    }
}

record ProjectReport
{
    public required string ProjectName { get; init; }
    public required string ProjectPath { get; init; }
    public required List<PackageInfo> DeclaredPackages { get; init; }
    public required List<PackageInfo> DirectUsedPackages { get; init; }
    public required List<PackageInfo> TransitiveUsedPackages { get; init; }
    public required List<PackageInfo> MissingDirectPackages { get; init; }
}

record PackageInfo(string Id, string Version);

record PackageUsage(string Id, string Version);

sealed record FrameworkFilterConfig
{
    public List<string>? IgnorePackages { get; init; }
    public List<string>? IgnorePrefixes { get; init; }
}

sealed class FrameworkFilter
{
    private readonly HashSet<string> _packages;
    private readonly List<string> _prefixes;

    public static FrameworkFilter Empty { get; } = new FrameworkFilter(null, null);

    public FrameworkFilter(IEnumerable<string>? packages, IEnumerable<string>? prefixes)
    {
        _packages = new HashSet<string>(packages ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        _prefixes = (prefixes ?? Array.Empty<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
    }

    public bool ShouldIgnore(string packageId)
    {
        if (_packages.Contains(packageId))
        {
            return true;
        }

        foreach (var prefix in _prefixes)
        {
            if (packageId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
