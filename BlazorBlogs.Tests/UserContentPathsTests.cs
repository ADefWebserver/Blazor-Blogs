using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorBlogs.Tests;

public sealed class UserContentPathsTests
{
    [Fact]
    public void ConfiguredRoot_UsesPersistentPublicAndPrivateFolders()
    {
        var root = Path.Combine(
            AppContext.BaseDirectory,
            "user-content",
            Guid.NewGuid().ToString("N"));
        var webRoot = Path.Combine(root, "web");
        Directory.CreateDirectory(webRoot);

        try
        {
            var environment = new TestEnvironment(root, webRoot);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:UserContentPath"] = Path.Combine(root, "persistent")
                })
                .Build();

            var paths = new UserContentPaths(environment, configuration);

            Assert.Equal(
                Path.Combine(root, "persistent", "public", "uploads"),
                paths.UploadsPath);
            Assert.Equal(
                Path.Combine(root, "persistent", "private", "downloads"),
                paths.DownloadsPath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EnsureFoldersAndSeed_SeedsDefaultsWithoutReplacingExistingFiles()
    {
        var root = Path.Combine(
            AppContext.BaseDirectory,
            "user-content",
            Guid.NewGuid().ToString("N"));
        var webRoot = Path.Combine(root, "web");
        var persistentRoot = Path.Combine(root, "persistent");
        var seed = Path.Combine(root, "SeedContent", "uploads");
        Directory.CreateDirectory(webRoot);
        Directory.CreateDirectory(seed);
        File.WriteAllText(Path.Combine(seed, "logo.png"), "default");

        try
        {
            var existingUpload = Path.Combine(persistentRoot, "public", "uploads", "logo.png");
            Directory.CreateDirectory(Path.GetDirectoryName(existingUpload)!);
            File.WriteAllText(existingUpload, "replacement");

            var environment = new TestEnvironment(root, webRoot);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:UserContentPath"] = persistentRoot
                })
                .Build();
            var paths = new UserContentPaths(environment, configuration);

            paths.EnsureFoldersAndSeed(NullLogger.Instance);

            Assert.Equal("replacement", File.ReadAllText(existingUpload));
            Assert.True(Directory.Exists(Path.Combine(persistentRoot, "public", "blogs")));
            Assert.True(Directory.Exists(paths.DownloadsPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class TestEnvironment(string contentRootPath, string webRootPath)
        : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "BlazorBlogs.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
