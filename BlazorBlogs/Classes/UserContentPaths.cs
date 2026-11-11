namespace BlazorBlogs;

public sealed class UserContentPaths
{
    private readonly IWebHostEnvironment _environment;

    public UserContentPaths(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        var configuredRoot = Environment.ExpandEnvironmentVariables(
            configuration["Storage:UserContentPath"] ?? "");

        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            PublicRoot = Path.GetFullPath(environment.WebRootPath);
            UploadsPath = Path.Combine(PublicRoot, "uploads");
            BlogsPath = Path.Combine(PublicRoot, "blogs");
            DownloadsPath = Path.Combine(environment.ContentRootPath, "PrivateFiles");
        }
        else
        {
            configuredRoot = Path.GetFullPath(configuredRoot);
            PublicRoot = Path.Combine(configuredRoot, "public");
            UploadsPath = Path.Combine(PublicRoot, "uploads");
            BlogsPath = Path.Combine(PublicRoot, "blogs");
            DownloadsPath = Path.Combine(configuredRoot, "private", "downloads");
        }
    }

    public string PublicRoot { get; }
    public string UploadsPath { get; }
    public string BlogsPath { get; }
    public string DownloadsPath { get; }

    public string ToRelativeUrl(string physicalPath)
    {
        return Path.GetRelativePath(PublicRoot, physicalPath).Replace('\\', '/');
    }

    public string ResolvePublicPath(string relativeUrl)
    {
        var normalized = relativeUrl
            .Replace('\\', '/')
            .TrimStart('/');
        var resolved = Path.GetFullPath(Path.Combine(PublicRoot, normalized));
        var publicPrefix = PublicRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!resolved.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(resolved, PublicRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The requested public content path is outside the configured root.");
        }

        return resolved;
    }

    public void EnsureFoldersAndSeed(ILogger logger)
    {
        Directory.CreateDirectory(UploadsPath);
        Directory.CreateDirectory(BlogsPath);
        Directory.CreateDirectory(DownloadsPath);

        if (!string.Equals(PublicRoot, _environment.WebRootPath, StringComparison.OrdinalIgnoreCase))
        {
            CopyMissingTree(
                Path.Combine(_environment.WebRootPath, "uploads"),
                UploadsPath,
                logger);
            CopyMissingTree(
                Path.Combine(_environment.WebRootPath, "blogs"),
                BlogsPath,
                logger);
        }

        CopyMissingTree(
            Path.Combine(_environment.ContentRootPath, "SeedContent", "uploads"),
            UploadsPath,
            logger);
    }

    private static void CopyMissingTree(string source, string destination, ILogger logger)
    {
        if (!Directory.Exists(source))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            try
            {
                var relative = Path.GetRelativePath(source, file);
                var target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (!File.Exists(target))
                {
                    File.Copy(file, target);
                    logger.LogInformation("Seeded user content {Target}.", target);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not seed user content {File}.", file);
            }
        }
    }
}
