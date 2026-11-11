namespace BlazorBlogs.Data;

public sealed class DownloadStorageService
{
    private readonly UserContentPaths _paths;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DownloadStorageService> _logger;

    public DownloadStorageService(
        UserContentPaths paths,
        IWebHostEnvironment environment,
        ILogger<DownloadStorageService> logger)
    {
        _paths = paths;
        _environment = environment;
        _logger = logger;
    }

    public string Resolve(string storedName)
    {
        var fileName = GetFileName(storedName);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("The download has no stored file name.");

        return Path.Combine(_paths.DownloadsPath, fileName);
    }

    public static string GetFileName(string value) =>
        Path.GetFileName((value ?? "").Replace('\\', '/'));

    public async Task<(string StoredName, string OriginalName)> SaveAsync(IFormFile file)
    {
        Directory.CreateDirectory(_paths.DownloadsPath);
        var storedName = $"{Guid.NewGuid():N}.zip";
        var path = Resolve(storedName);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await file.CopyToAsync(output);
        return (storedName, Path.GetFileName(file.FileName));
    }

    public void Delete(string storedName)
    {
        var path = Resolve(storedName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public void MigrateLegacyFiles()
    {
        var legacyPaths = new[]
        {
            Path.Combine(_environment.WebRootPath, "files"),
            Path.Combine(_environment.ContentRootPath, "PrivateFiles")
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceDirectory in legacyPaths)
        {
            try
            {
                if (!Directory.Exists(sourceDirectory))
                    continue;

                foreach (var source in Directory.EnumerateFiles(sourceDirectory))
                {
                    var target = Path.Combine(_paths.DownloadsPath, Path.GetFileName(source));
                    try
                    {
                        if (!File.Exists(target))
                        {
                            Directory.CreateDirectory(_paths.DownloadsPath);
                            File.Move(source, target);
                            _logger.LogInformation("Moved legacy download {Source} to private storage.", source);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not move legacy download {Source}.", source);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not inspect legacy download directory {Directory}.", sourceDirectory);
            }
        }
    }
}
