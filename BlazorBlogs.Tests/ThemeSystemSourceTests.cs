using System.Text.RegularExpressions;
using BlazorBlogs.Data;
using BlazorBlogs.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorBlogs.Tests;

public sealed class ThemeSystemSourceTests
{
    [Fact]
    public void ThemeBoundary_IsAdministratorOnlyAndAntiforgeryProtected()
    {
        var root = FindRepositoryRoot();
        var controller = File.ReadAllText(
            System.IO.Path.Combine(root, "BlazorBlogs", "Controllers", "ThemeController.cs"));
        var page = File.ReadAllText(
            System.IO.Path.Combine(root, "BlazorBlogs", "Components", "Pages", "ThemePreview.razor"));

        Assert.Contains("[Authorize(Roles = \"Administrators\")]", controller);
        Assert.Contains("[AutoValidateAntiforgeryToken]", controller);
        Assert.Contains("[HttpPost(\"import\")]", controller);
        Assert.Contains("Status413PayloadTooLarge", controller);
        Assert.Contains("StatusCode", controller);
        Assert.Contains("[HttpDelete(\"{id:guid}\")]", controller);
        Assert.Contains("[Authorize(Roles = \"Administrators\")]", page);
        Assert.Contains("noindex", page);
    }

    [Fact]
    public void App_AppendsThemeAfterBaseStylesAndHandlesFallback()
    {
        var root = FindRepositoryRoot();
        var app = File.ReadAllText(System.IO.Path.Combine(root, "BlazorBlogs", "Components", "App.razor"));
        var radzen = app.IndexOf("<RadzenTheme", StringComparison.Ordinal);
        var link = app.IndexOf("data-theme-package=\"true\"", StringComparison.Ordinal);

        Assert.True(radzen >= 0 && link > radzen);
        Assert.Contains("GetActiveStylesheetUrlAsync", app);
        Assert.Contains("Theme stylesheet is unavailable; using built-in styles.", app);
        Assert.Contains("GetStylesheetUrlAsync(previewId)", app);
    }

    [Fact]
    public void ThemeStaticSource_IsPersistentAndExcludedFromPublish()
    {
        var root = FindRepositoryRoot();
        var project = File.ReadAllText(System.IO.Path.Combine(root, "BlazorBlogs", "BlazorBlogs.csproj"));
        var paths = File.ReadAllText(System.IO.Path.Combine(root, "BlazorBlogs", "Classes", "UserContentPaths.cs"));
        var program = File.ReadAllText(System.IO.Path.Combine(root, "BlazorBlogs", "Program.cs"));

        Assert.Contains("ThemesPath", paths);
        Assert.Matches(
            new Regex(
                @"ThemesPath = Path\.Combine\(PublicRoot,\s*""themes""\)",
                RegexOptions.CultureInvariant),
            paths);
        Assert.Contains(@"Content Update=""wwwroot\themes\**\*""", project);
        Assert.Contains("<CopyToPublishDirectory>Never</CopyToPublishDirectory>", project);
        Assert.Contains("new PhysicalFileProvider(userContentPaths.PublicRoot)", program);
        Assert.Single(
            Regex.Matches(program, "new PhysicalFileProvider\\(userContentPaths\\.PublicRoot\\)")
                .Cast<Match>());
    }

    [Fact]
    public void ThemeEntity_HasRequiredLengthsAndUniqueIdentity()
    {
        var options = new DbContextOptionsBuilder<BlazorBlogsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new BlazorBlogsContext(options);
        var entity = context.Model.FindEntityType(typeof(ThemePackage));

        Assert.NotNull(entity);
        Assert.Equal("ThemePackages", entity.GetTableName());
        Assert.Equal(100, entity.FindProperty(nameof(ThemePackage.Name))!.GetMaxLength());
        Assert.Equal(50, entity.FindProperty(nameof(ThemePackage.Slug))!.GetMaxLength());
        Assert.Equal(32, entity.FindProperty(nameof(ThemePackage.Version))!.GetMaxLength());
        Assert.Contains(
            entity.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(ThemePackage.Slug), nameof(ThemePackage.Version)]));
    }

    [Fact]
    public void ThemeManager_UsesEveryLifecycleEndpointAndPreviewIsRequestScoped()
    {
        var root = FindRepositoryRoot();
        var manager = File.ReadAllText(
            System.IO.Path.Combine(root, "BlazorBlogs", "Components", "ThemeManager.razor"));

        Assert.Contains("/api/themes/import", manager);
        Assert.Contains("/activate", manager);
        Assert.Contains("/api/themes/deactivate", manager);
        Assert.Contains("data-api-method=\"DELETE\"", manager);
        Assert.Contains("__RequestVerificationToken", manager);
        Assert.Contains("/settings/themes/preview/", manager);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "BlazorBlogs.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Could not locate the repository root.");
    }
}
