using BlazorBlogs.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorBlogs.Data;

public sealed class ThemePackageService(
    BlazorBlogsContext context,
    ThemePackageValidator validator,
    UserContentPaths paths,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ThemePackageService> logger) : IThemePackageService
{
    private static readonly TimeSpan OrphanGracePeriod = TimeSpan.FromHours(1);

    public async Task<ThemePackage> ImportAsync(
        Stream archive,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        if (archive is null || string.IsNullOrWhiteSpace(fileName))
            throw new ThemePackageValidationException("Select a theme ZIP archive.");
        if (!string.Equals(Path.GetExtension(fileName), ".zip", StringComparison.OrdinalIgnoreCase))
            throw new ThemePackageValidationException("Only .zip theme archives are accepted.");

        Directory.CreateDirectory(paths.ThemesPath);
        var id = Guid.NewGuid();
        var stagingPath = Path.Combine(paths.ThemesPath, $".staging-{id:D}");
        var archivePath = stagingPath + ".zip";
        string finalPath = null;

        try
        {
            await using (var archiveFile = new FileStream(
                archivePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous))
            {
                await CopyArchiveBoundedAsync(archive, archiveFile, cancellationToken);
            }

            ValidatedThemePackage validated;
            try
            {
                validated = await validator.ExtractAsync(
                    archivePath,
                    stagingPath,
                    cancellationToken);
            }
            catch (InvalidDataException)
            {
                throw new ThemePackageValidationException("The uploaded ZIP archive is invalid.");
            }
            var manifest = validated.Manifest;
            if (await context.ThemePackages.AnyAsync(
                    item => item.Slug == manifest.Slug && item.Version == manifest.Version,
                    cancellationToken))
            {
                throw new ThemePackageConflictException(
                    "A package with this slug and version is already installed.");
            }

            var slugPath = EnsureSafeChild(paths.ThemesPath, manifest.Slug);
            Directory.CreateDirectory(slugPath);
            finalPath = EnsureSafeChild(slugPath, id.ToString("D"));
            Directory.Move(stagingPath, finalPath);

            var package = new ThemePackage
            {
                ThemePackageId = id,
                Name = manifest.Name,
                Slug = manifest.Slug,
                Version = manifest.Version,
                ManifestJson = validated.ManifestJson,
                StylesheetUrl = $"/themes/{manifest.Slug}/{id:D}/theme.css",
                CreatedUtc = DateTime.UtcNow
            };
            context.ThemePackages.Add(package);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                context.Entry(package).State = EntityState.Detached;
                DeleteDirectory(finalPath);
                DeleteDirectoryIfEmpty(Path.GetDirectoryName(finalPath)!);
                finalPath = null;
                throw;
            }

            logger.LogInformation(
                "Imported inactive theme package {PackageId} ({Slug} {Version}).",
                id,
                manifest.Slug,
                manifest.Version);
            return package;
        }
        catch
        {
            DeleteDirectory(stagingPath);
            if (finalPath is not null && Directory.Exists(finalPath)
                && !context.ThemePackages.Local.Any(package => package.ThemePackageId == id))
            {
                DeleteDirectory(finalPath);
                DeleteDirectoryIfEmpty(Path.GetDirectoryName(finalPath)!);
            }
            throw;
        }
        finally
        {
            if (File.Exists(archivePath))
                File.Delete(archivePath);
        }
    }

    public async Task<IReadOnlyList<ThemePackage>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await context.ThemePackages
            .AsNoTracking()
            .OrderByDescending(package => package.CreatedUtc)
            .ThenBy(package => package.Name)
            .ToListAsync(cancellationToken);

    public Task<ThemePackage> GetAsync(
        Guid packageId,
        CancellationToken cancellationToken = default) =>
        context.ThemePackages
            .AsNoTracking()
            .SingleOrDefaultAsync(package => package.ThemePackageId == packageId, cancellationToken);

    public async Task<string> GetStylesheetUrlAsync(
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package = await GetAsync(packageId, cancellationToken);
        if (package is null)
            return null;
        try
        {
            return await ValidatePackageAsync(package, cancellationToken);
        }
        catch (Exception ex) when (ex is ThemePackageValidationException
                                       or IOException
                                       or UnauthorizedAccessException)
        {
            logger.LogWarning(
                ex,
                "Theme package {PackageId} failed integrity validation.",
                packageId);
            throw;
        }
    }

    public async Task ActivateAsync(
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        var package = await GetAsync(packageId, cancellationToken)
            ?? throw new KeyNotFoundException("Theme package was not found.");
        await ValidatePackageAsync(package, cancellationToken);
        await SetActiveSettingAsync(package.ThemePackageId.ToString("D"), cancellationToken);
        logger.LogInformation("Activated theme package {PackageId}.", packageId);
    }

    public async Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        await SetActiveSettingAsync("", cancellationToken);
        logger.LogInformation("Deactivated the theme package.");
    }

    public async Task DeleteAsync(
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        var package = await context.ThemePackages
            .SingleOrDefaultAsync(item => item.ThemePackageId == packageId, cancellationToken);
        if (package is null)
            return;

        var activeId = await GetActiveIdAsync(cancellationToken);
        if (string.Equals(activeId, packageId.ToString("D"), StringComparison.OrdinalIgnoreCase))
            throw new ThemePackageConflictException(
                "Deactivate this package before deleting it.");

        var packagePath = GetPackagePath(package);
        context.ThemePackages.Remove(package);
        await context.SaveChangesAsync(cancellationToken);
        DeleteDirectory(packagePath);
        logger.LogInformation("Deleted theme package {PackageId}.", packageId);
    }

    public async Task<string> GetActiveStylesheetUrlAsync(
        CancellationToken cancellationToken = default)
    {
        var value = await GetActiveIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!Guid.TryParseExact(value, "D", out var id)
            || !string.Equals(value, id.ToString("D"), StringComparison.Ordinal))
        {
            logger.LogWarning("The active theme setting is not a canonical package ID.");
            return null;
        }

        var stylesheetUrl = await GetStylesheetUrlAsync(id, cancellationToken);
        if (stylesheetUrl is null)
            logger.LogWarning(
                "Active theme package {PackageId} has no registry entry; using built-in styles.",
                id);
        return stylesheetUrl;
    }

    public async Task<Guid?> GetActivePackageIdAsync(
        CancellationToken cancellationToken = default)
    {
        var value = await GetActiveIdAsync(cancellationToken);
        return Guid.TryParseExact(value, "D", out var id)
            && string.Equals(value, id.ToString("D"), StringComparison.Ordinal)
                ? id
                : null;
    }

    public async Task ReconcileOrphansAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.ThemesPath);
        var cutoff = DateTime.UtcNow - OrphanGracePeriod;
        var registered = await context.ThemePackages
            .AsNoTracking()
            .Select(package => package.ThemePackageId)
            .ToListAsync(cancellationToken);
        var ids = registered.ToHashSet();

        foreach (var entry in Directory.EnumerateFileSystemEntries(paths.ThemesPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(entry);
            if (name.StartsWith(".staging-", StringComparison.Ordinal))
            {
                if (Directory.GetLastWriteTimeUtc(entry) < cutoff)
                    DeleteFileOrDirectory(entry);
                continue;
            }

            if (!Directory.Exists(entry))
                continue;
            if ((File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0
                || !IsWithinThemesRoot(entry))
            {
                logger.LogWarning("Skipped unsafe theme orphan path {Path}.", name);
                continue;
            }
            foreach (var packagePath in Directory.EnumerateDirectories(entry))
            {
                if (!IsWithinThemesRoot(packagePath)
                    || (File.GetAttributes(packagePath) & FileAttributes.ReparsePoint) != 0)
                {
                    logger.LogWarning("Skipped unsafe nested theme package path.");
                    continue;
                }
                if (!Guid.TryParseExact(Path.GetFileName(packagePath), "D", out var id)
                    || ids.Contains(id))
                {
                    continue;
                }

                if (Directory.GetLastWriteTimeUtc(packagePath) < cutoff)
                {
                    DeleteDirectory(packagePath);
                    logger.LogWarning(
                        "Removed orphaned theme package folder {PackageId} older than one hour.",
                        id);
                }
            }
        }
    }

    private async Task<string> ValidatePackageAsync(
        ThemePackage package,
        CancellationToken cancellationToken)
    {
        var manifest = validator.ParseManifest(package.ManifestJson);
        var expected = new ThemeManifest(
            package.Name,
            package.Slug,
            package.Version,
            "theme.css",
            manifest.Assets);
        if (!ThemePackageValidator.ManifestsEqual(manifest, expected))
            throw new ThemePackageValidationException("Theme registry and manifest do not match.");

        var packagePath = GetPackagePath(package);
        await validator.ValidateInstalledAsync(
            packagePath,
            manifest,
            package.ManifestJson,
            cancellationToken);
        var expectedUrl = BuildStylesheetUrl(package);
        if (!string.Equals(package.StylesheetUrl, expectedUrl, StringComparison.Ordinal))
            throw new ThemePackageValidationException("Theme stylesheet URL is invalid.");
        return expectedUrl;
    }

    private string GetPackagePath(ThemePackage package)
    {
        var slugPath = EnsureSafeChild(paths.ThemesPath, package.Slug);
        RejectReparsePoint(slugPath);
        var packagePath = EnsureSafeChild(slugPath, package.ThemePackageId.ToString("D"));
        RejectReparsePoint(packagePath);
        return packagePath;
    }

    private static string BuildStylesheetUrl(ThemePackage package) =>
        $"/themes/{package.Slug}/{package.ThemePackageId:D}/theme.css";

    private string EnsureSafeChild(string parent, string child)
    {
        var result = Path.GetFullPath(Path.Combine(parent, child));
        if (!result.StartsWith(
                Path.GetFullPath(paths.ThemesPath).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ThemePackageValidationException("Theme package path is outside the themes root.");
        }
        return result;
    }

    private async Task SetActiveSettingAsync(
        string value,
        CancellationToken cancellationToken)
    {
        var setting = await context.Settings
            .SingleOrDefaultAsync(
                item => item.SettingName == "ActiveThemePackageId",
                cancellationToken);
        if (setting is null)
        {
            setting = new Settings { SettingName = "ActiveThemePackageId" };
            context.Settings.Add(setting);
        }

        setting.SettingValue = value;
        await context.SaveChangesAsync(cancellationToken);
    }

    private Task<string> GetActiveIdAsync(CancellationToken cancellationToken) =>
        context.Settings
            .AsNoTracking()
            .Where(item => item.SettingName == "ActiveThemePackageId")
            .Select(item => item.SettingValue)
            .SingleOrDefaultAsync(cancellationToken);

    private static async Task CopyArchiveBoundedAsync(
        Stream input,
        Stream output,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) != 0)
        {
            total += read;
            if (total > ThemePackageValidator.MaxArchiveBytes)
                throw new ThemePackageTooLargeException(
                    "Theme archive exceeds the 10 MiB limit.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            Directory.Delete(path);
            return;
        }
        foreach (var entry in Directory.EnumerateFileSystemEntries(path))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                if (Directory.Exists(entry))
                    Directory.Delete(entry);
                else
                    File.Delete(entry);
                continue;
            }

            if (Directory.Exists(entry))
                DeleteDirectory(entry);
            else
            {
                File.SetAttributes(entry, FileAttributes.Normal);
                File.Delete(entry);
            }
        }
        Directory.Delete(path);
    }

    private static void DeleteDirectoryIfEmpty(string path)
    {
        if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            Directory.Delete(path);
    }

    private static void RejectReparsePoint(string path)
    {
        if (Directory.Exists(path)
            && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new ThemePackageValidationException(
                "Theme package path contains a symbolic link or reparse point.");
    }

    private bool IsWithinThemesRoot(string path)
    {
        var root = Path.GetFullPath(paths.ThemesPath).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static void DeleteFileOrDirectory(string path)
    {
        if (Directory.Exists(path))
            DeleteDirectory(path);
        else if (File.Exists(path))
            File.Delete(path);
    }

    private void RequireAdministrator()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole("Administrators"))
        {
            throw new UnauthorizedAccessException("Administrator access is required.");
        }
    }
}
