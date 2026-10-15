using System.Xml.Linq;
using RassAppControleFin.Domain.Common;
using Xunit;

namespace RassAppControleFin.ArchitectureTests;

public sealed class DependencyTests
{
    private static readonly IReadOnlyDictionary<string, string[]> Allowed = new Dictionary<string, string[]>
    {
        ["Domain"] = [], ["SharedKernel"] = [], ["Contracts"] = [],
        ["Security"] = [], ["MultiTenancy"] = [], ["EventBus"] = [],
        ["AI"] = ["SharedKernel"], ["Synchronization"] = ["SharedKernel"],
        ["Application"] = ["Domain", "SharedKernel", "Security", "MultiTenancy", "Synchronization", "AI", "EventBus"],
        ["Infrastructure"] = ["Application"],
        ["Api"] = ["Application", "Infrastructure", "Contracts"],
        ["Web"] = ["Contracts"], ["Admin.Web"] = ["Contracts"]
    };

    [Fact]
    public void Every_project_respects_the_allowed_dependency_graph()
    {
        var root = FindRoot();
        foreach (var (name, allowed) in Allowed)
        {
            var document = XDocument.Load(Path.Combine(root, "src",
                "RassAppControleFin." + name, "RassAppControleFin." + name + ".csproj"));
            foreach (var reference in document.Descendants("ProjectReference"))
            {
                var target = Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value
                    .Replace('\\', '/')).Replace("RassAppControleFin.", "");
                Assert.Contains(target, allowed);
            }

            if (name is "Domain" or "Contracts" or "SharedKernel")
            {
                Assert.Empty(document.Descendants("PackageReference"));
                Assert.Empty(document.Descendants("FrameworkReference"));
            }
        }
    }

    [Fact]
    public void Domain_assembly_contains_no_provider_or_framework_dependency()
    {
        var assemblies = typeof(Entity).Assembly.GetReferencedAssemblies();
        Assert.All(assemblies, assembly =>
            Assert.True(assembly.Name!.StartsWith("System", StringComparison.Ordinal)
                || assembly.Name == "netstandard", assembly.Name));
    }

    [Fact]
    public void Server_projects_do_not_reference_sqlserver_or_sqlite()
    {
        foreach (var path in Directory.EnumerateFiles(Path.Combine(FindRoot(), "src"), "*.csproj", SearchOption.AllDirectories))
        {
            var packages = XDocument.Load(path).Descendants("PackageReference")
                .Select(node => node.Attribute("Include")!.Value);
            Assert.DoesNotContain(packages, package => package.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)
                || package.Contains("Sqlite", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Database_packages_are_confined_to_infrastructure()
    {
        foreach (var path in Directory.EnumerateFiles(Path.Combine(FindRoot(), "src"), "*.csproj", SearchOption.AllDirectories))
        {
            if (path.Contains("RassAppControleFin.Infrastructure", StringComparison.Ordinal)) continue;
            var packages = XDocument.Load(path).Descendants("PackageReference")
                .Select(node => node.Attribute("Include")!.Value);
            Assert.DoesNotContain(packages, package => package.StartsWith("Npgsql", StringComparison.Ordinal)
                || package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Api_startup_cannot_apply_migrations_or_load_migration_credentials()
    {
        var root = FindRoot();
        foreach (var path in new[]
        {
            Path.Combine(root, "src/RassAppControleFin.Api/Program.cs"),
            Path.Combine(root, "src/RassAppControleFin.Infrastructure/DependencyInjection.cs")
        })
        {
            var code = File.ReadAllText(path);
            Assert.DoesNotContain("RASS_MIGRATIONS_CONNECTION", code, StringComparison.Ordinal);
            Assert.DoesNotContain(".Migrate(", code, StringComparison.Ordinal);
            Assert.DoesNotContain(".MigrateAsync(", code, StringComparison.Ordinal);
        }
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RassAppControleFin.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Solution root not found.");
    }
}
