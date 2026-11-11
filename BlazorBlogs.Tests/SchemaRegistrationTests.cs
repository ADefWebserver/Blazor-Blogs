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

        Assert.Contains("const string CurrentVersion = \"10.05.00\"", File.ReadAllText(layoutPath));
        foreach (var version in new[] { "10.00.00", "10.01.00", "10.02.00", "10.03.00", "10.04.00", "10.05.00" })
        {
            Assert.True(File.Exists(Path.Combine(root, "BlazorBlogs", "SQLScripts", $"{version}.sql")));
            Assert.Contains(version, runnerText);
            Assert.Contains(
                project.Descendants("None").Where(element =>
                    element.Attribute("Update")?.Value.Contains($"{version}.sql") == true),
                element => element
                    .Elements("CopyToOutputDirectory")
                    .Any(child => child.Value == "Always"));
        }

        Assert.DoesNotContain("UpgradeAsync", File.ReadAllText(Path.Combine(
            root, "BlazorBlogs", "Controllers", "UploadController.cs")));
        Assert.Contains(@"wwwroot\uploads\**\*", projectText);
        Assert.Contains(@"wwwroot\blogs\**\*", projectText);
        Assert.Contains(@"wwwroot\files\**\*", projectText);
        Assert.Contains(@"PrivateFiles\**\*", projectText);
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
