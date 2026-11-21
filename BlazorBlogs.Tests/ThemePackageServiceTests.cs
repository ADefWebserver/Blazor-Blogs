using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using BlazorBlogs.Data;
using BlazorBlogs.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorBlogs.Tests;

public sealed class ThemePackageServiceTests
{
    [Fact]
    public async Task ImportActivateDeactivateAndDelete_UpdatesPersistentState()
    {
        var fixture = new ThemeFixture();
        try
        {
            await using var archive = ValidArchive();
            var package = await fixture.Service.ImportAsync(archive, "example.zip");

            Assert.Null(await fixture.Service.GetActivePackageIdAsync());
            Assert.True(Directory.Exists(fixture.PackagePath(package)));
            Assert.Single(await fixture.Service.ListAsync());

            await fixture.Service.ActivateAsync(package.ThemePackageId);
            Assert.Equal(package.ThemePackageId, await fixture.Service.GetActivePackageIdAsync());
            Assert.Equal(package.StylesheetUrl, await fixture.Service.GetActiveStylesheetUrlAsync());

            await Assert.ThrowsAsync<ThemePackageConflictException>(
                () => fixture.Service.DeleteAsync(package.ThemePackageId));

            await fixture.Service.DeactivateAsync();
            await fixture.Service.DeleteAsync(package.ThemePackageId);
            Assert.Empty(await fixture.Service.ListAsync());
            Assert.False(Directory.Exists(fixture.PackagePath(package)));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task IntegrityFailure_DoesNotReplaceActiveTheme()
    {
        var fixture = new ThemeFixture();
        try
        {
            await using var firstArchive = ValidArchive("first", "1.0.0");
            var first = await fixture.Service.ImportAsync(firstArchive, "first.zip");
            await fixture.Service.ActivateAsync(first.ThemePackageId);

            await using var secondArchive = ValidArchive("second", "2.0.0");
            var second = await fixture.Service.ImportAsync(secondArchive, "second.zip");
            File.WriteAllText(
                Path.Combine(fixture.PackagePath(second), "theme.css"),
                "@import 'https://example.invalid/theme.css';");

            await Assert.ThrowsAsync<ThemePackageValidationException>(
                () => fixture.Service.ActivateAsync(second.ThemePackageId));
            Assert.Equal(first.ThemePackageId, await fixture.Service.GetActivePackageIdAsync());
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task InvalidImport_LeavesNoPartialFolderOrDatabaseRow()
    {
        var fixture = new ThemeFixture();
        try
        {
            await using var archive = new MemoryStream(Encoding.UTF8.GetBytes("not a zip"));

            await Assert.ThrowsAsync<ThemePackageValidationException>(
                () => fixture.Service.ImportAsync(archive, "invalid.zip"));
            Assert.Empty(await fixture.Service.ListAsync());
            Assert.DoesNotContain(
                Directory.EnumerateDirectories(fixture.Paths.ThemesPath, "*", SearchOption.AllDirectories),
                path => Path.GetFileName(path).StartsWith(".staging-", StringComparison.Ordinal));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task OversizedCompressedStream_IsRejectedBeforeExtraction()
    {
        var fixture = new ThemeFixture();
        try
        {
            var oversized = new MemoryStream(
                new byte[checked((int)ThemePackageValidator.MaxArchiveBytes) + 1]);

            await Assert.ThrowsAsync<ThemePackageTooLargeException>(
                () => fixture.Service.ImportAsync(oversized, "large.zip"));
            Assert.Empty(await fixture.Service.ListAsync());
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task DatabaseFailure_RollsBackCanonicalFolder()
    {
        var fixture = new ThemeFixture(failPackageSave: true);
        try
        {
            await using var archive = ValidArchive();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.ImportAsync(archive, "example.zip"));
            Assert.Empty(await fixture.Context.ThemePackages.ToListAsync());
            Assert.Empty(Directory.EnumerateDirectories(
                fixture.Paths.ThemesPath,
                "*",
                SearchOption.AllDirectories));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task OrphanFolder_IsRemovedOnlyAfterGracePeriod()
    {
        var fixture = new ThemeFixture();
        try
        {
            var oldId = Guid.NewGuid();
            var oldPath = System.IO.Path.Combine(
                fixture.Paths.ThemesPath,
                "orphan",
                oldId.ToString("D"));
            Directory.CreateDirectory(oldPath);
            Directory.SetLastWriteTimeUtc(oldPath, DateTime.UtcNow.AddHours(-2));

            var youngId = Guid.NewGuid();
            var youngPath = System.IO.Path.Combine(
                fixture.Paths.ThemesPath,
                "orphan",
                youngId.ToString("D"));
            Directory.CreateDirectory(youngPath);

            await fixture.Service.ReconcileOrphansAsync();

            Assert.False(Directory.Exists(oldPath));
            Assert.True(Directory.Exists(youngPath));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task RegistryAndActiveSetting_PersistAcrossContexts()
    {
        var fixture = new ThemeFixture();
        try
        {
            await using var archive = ValidArchive();
            var package = await fixture.Service.ImportAsync(archive, "persistent.zip");
            await fixture.Service.ActivateAsync(package.ThemePackageId);

            await using var newContext = new BlazorBlogsContext(fixture.Options);
            var restartedService = new ThemePackageService(
                newContext,
                new ThemePackageValidator(),
                fixture.Paths,
                fixture.AdminHttpContextAccessor,
                NullLogger<ThemePackageService>.Instance);

            Assert.Equal(package.StylesheetUrl, await restartedService.GetActiveStylesheetUrlAsync());
            Assert.Single(await restartedService.ListAsync());
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Mutations_RequireAdministratorAtTheServiceBoundary()
    {
        var fixture = new ThemeFixture();
        try
        {
            var anonymousService = new ThemePackageService(
                fixture.Context,
                new ThemePackageValidator(),
                fixture.Paths,
                new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
                NullLogger<ThemePackageService>.Instance);
            await using var archive = ValidArchive();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => anonymousService.ImportAsync(archive, "example.zip"));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => anonymousService.ActivateAsync(Guid.NewGuid()));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => anonymousService.DeactivateAsync());
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => anonymousService.DeleteAsync(Guid.NewGuid()));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static MemoryStream ValidArchive(string slug = "example", string version = "1.0.0")
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "theme.json", $$"""
                {"name":"Example","slug":"{{slug}}","version":"{{version}}","stylesheet":"theme.css","assets":["assets/example.png"]}
                """);
            Add(zip, "theme.css", "body { color: black; background: url(assets/example.png); }");
            var asset = zip.CreateEntry("assets/example.png");
            using var assetStream = asset.Open();
            assetStream.Write([137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0]);
        }
        stream.Position = 0;
        return stream;
    }

    private static void Add(ZipArchive zip, string path, string content)
    {
        using var stream = zip.CreateEntry(path).Open();
        stream.Write(Encoding.UTF8.GetBytes(content));
    }

    private sealed class ThemeFixture : IAsyncDisposable
    {
        private readonly TestRoot _root = TestRoot.Create();
        public BlazorBlogsContext Context { get; }
        public UserContentPaths Paths { get; }
        public IHttpContextAccessor AdminHttpContextAccessor { get; }
        public ThemePackageService Service { get; }
        public DbContextOptions<BlazorBlogsContext> Options { get; }

        public ThemeFixture(bool failPackageSave = false)
        {
            Options = new DbContextOptionsBuilder<BlazorBlogsContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            Context = failPackageSave
                ? new FailingThemeContext(Options)
                : new BlazorBlogsContext(Options);

            var webRoot = System.IO.Path.Combine(_root.Path, "web");
            Directory.CreateDirectory(webRoot);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:UserContentPath"] = System.IO.Path.Combine(_root.Path, "persistent")
                })
                .Build();
            var environment = new TestWebEnvironment(_root.Path, webRoot);
            Paths = new UserContentPaths(environment, configuration);
            Paths.EnsureFoldersAndSeed(NullLogger.Instance);
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.Name, "administrator"),
                            new Claim(ClaimTypes.Role, "Administrators")
                        ],
                        "Test"))
            };
            AdminHttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            Service = new ThemePackageService(
                Context,
                new ThemePackageValidator(),
                Paths,
                AdminHttpContextAccessor,
                NullLogger<ThemePackageService>.Instance);
        }

        public string PackagePath(ThemePackage package) =>
            System.IO.Path.Combine(
                Paths.ThemesPath,
                package.Slug,
                package.ThemePackageId.ToString("D"));

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            _root.Dispose();
        }
    }

    private sealed class FailingThemeContext(DbContextOptions<BlazorBlogsContext> options)
        : BlazorBlogsContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ChangeTracker.Entries<ThemePackage>().Any(entry => entry.State == EntityState.Added))
                throw new InvalidOperationException("Simulated database failure.");
            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class TestWebEnvironment(string contentRootPath, string webRootPath)
        : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "BlazorBlogs.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = contentRootPath;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
