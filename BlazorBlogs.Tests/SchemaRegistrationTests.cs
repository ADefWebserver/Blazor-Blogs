using System.Xml.Linq;

namespace BlazorBlogs.Tests;

public sealed class SchemaRegistrationTests
{
    [Fact]
    public void CurrentSchemaScripts_AreCopiedAndRegistered()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "BlazorBlogs", "BlazorBlogs.csproj");
        var runnerPath = Path.Combine(
            root,
            "BlazorBlogs",
            "Components",
            "Pages",
            "InstallUpgrade",
            "InstallWizardExecuteScripts.razor");
        var layoutPath = Path.Combine(
            root,
            "BlazorBlogs",
            "Components",
            "Layout",
            "MainLayout.razor");

        var project = XDocument.Load(projectPath);
        var projectText = File.ReadAllText(projectPath);
        var runnerText = File.ReadAllText(runnerPath);

        Assert.Contains("const string CurrentVersion = \"10.08.00\"", File.ReadAllText(layoutPath));
        foreach (var version in new[] { "10.00.00", "10.01.00", "10.02.00", "10.03.00", "10.04.00", "10.05.00", "10.06.00", "10.07.00", "10.08.00" })
        {
            Assert.True(File.Exists(Path.Combine(root, "BlazorBlogs", "SQLScripts", $"{version}.sql")));
            Assert.Contains(version, runnerText);
            var scriptElement = Assert.Single(
                project.Descendants("None"),
                element => element.Attribute("Update")?.Value.Contains($"{version}.sql") == true);
            Assert.Contains(
                scriptElement.Elements("CopyToOutputDirectory"),
                child => child.Value == "Always");
        }

        var searchScript = Assert.Single(
            project.Descendants("None"),
            element => element.Attribute("Update")?.Value == @"SQLScripts\10.07.00.sql");
        Assert.Contains(
            searchScript.Elements("CopyToPublishDirectory"),
            child => child.Value == "Always");

        var schema = File.ReadAllText(Path.Combine(
            root,
            "BlazorBlogs",
            "SQLScripts",
            "10.07.00.sql"));
        Assert.Contains("GoogleSiteSearchEnabled", schema, StringComparison.Ordinal);
        Assert.Contains("N'False'", schema, StringComparison.Ordinal);
        Assert.Contains("IF NOT EXISTS", schema, StringComparison.Ordinal);
        Assert.Contains("'10.07.00'", schema, StringComparison.Ordinal);

        var themeScript = Assert.Single(
            project.Descendants("None"),
            element => element.Attribute("Update")?.Value == @"SQLScripts\10.08.00.sql");
        Assert.Contains(
            themeScript.Elements("CopyToPublishDirectory"),
            child => child.Value == "Always");
        var themeSchema = File.ReadAllText(Path.Combine(
            root,
            "BlazorBlogs",
            "SQLScripts",
            "10.08.00.sql"));
        Assert.Contains("CREATE TABLE [dbo].[ThemePackages]", themeSchema, StringComparison.Ordinal);
        Assert.Contains("UNIQUE ([Slug], [Version])", themeSchema, StringComparison.Ordinal);
        Assert.Contains("ActiveThemePackageId", themeSchema, StringComparison.Ordinal);
        Assert.Contains("'10.08.00'", themeSchema, StringComparison.Ordinal);

        Assert.DoesNotContain("UpgradeAsync", File.ReadAllText(Path.Combine(
            root, "BlazorBlogs", "Controllers", "UploadController.cs")));
        Assert.Contains(@"wwwroot\uploads\**\*", projectText);
        Assert.Contains(@"wwwroot\blogs\**\*", projectText);
        Assert.Contains(@"wwwroot\files\**\*", projectText);
        Assert.Contains(@"PrivateFiles\**\*", projectText);
        Assert.Contains(@"wwwroot\themes\**\*", projectText);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BlazorBlogs.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }
}
