using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlazorBlogs.Data.Models;

namespace BlazorBlogs.Data;

public sealed record ValidatedThemePackage(ThemeManifest Manifest, string ManifestJson);

public sealed class ThemePackageValidator
{
    public const long MaxArchiveBytes = 10L * 1024 * 1024;
    public const long MaxExpandedBytes = 20L * 1024 * 1024;
    public const int MaxEntries = 250;
    public const long MaxStylesheetBytes = 1024 * 1024;
    public const long MaxManifestBytes = 32 * 1024;

    private static readonly HashSet<string> AssetExtensions = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ".avif", ".gif", ".jpeg", ".jpg", ".png", ".webp", ".woff", ".woff2"
    };

    private static readonly Regex SlugPattern =
        new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);

    private static readonly Regex VersionPattern =
        new("^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)$",
            RegexOptions.CultureInvariant);

    private static readonly Regex CssUrlPattern =
        new(@"url\s*\(\s*(?:(['""])(.*?)\1|([^)]*))\s*\)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public async Task<ValidatedThemePackage> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        using var archiveStream = new FileStream(
            archivePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        return await ExtractAsync(archiveStream, destinationPath, cancellationToken);
    }

    public async Task<ValidatedThemePackage> ExtractAsync(
        Stream archiveStream,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        if (!archiveStream.CanSeek)
            throw new ThemePackageValidationException("Theme archive stream must be seekable.");
        if (archiveStream.Length > MaxArchiveBytes)
            throw new ThemePackageValidationException("Theme archive exceeds the 10 MiB limit.");

        var signature = new byte[4];
        archiveStream.Position = 0;
        if (await archiveStream.ReadAsync(signature.AsMemory(0, 4), cancellationToken) != 4
            || !IsZipSignature(signature))
        {
            throw new ThemePackageValidationException("The uploaded file is not a ZIP archive.");
        }

        archiveStream.Position = 0;
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException)
        {
            throw new ThemePackageValidationException("The uploaded ZIP archive is invalid.");
        }

        await using (archive)
        {
            if (archive.Entries.Count > MaxEntries)
                throw new ThemePackageValidationException("Theme archive contains more than 250 entries.");

            var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            long expanded = 0;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = ValidateArchivePath(entry.FullName);
                if (!string.IsNullOrEmpty(path)
                    && !entries.TryAdd(path, entry))
                {
                    throw new ThemePackageValidationException(
                        $"Theme archive contains a duplicate path: {entry.FullName}.");
                }

                RejectSymlink(entry);
                if (entry.Length < 0)
                    throw new ThemePackageValidationException("Theme archive contains an invalid entry size.");

                expanded += entry.Length;
                if (expanded > MaxExpandedBytes)
                    throw new ThemePackageValidationException("Expanded theme content exceeds the 20 MiB limit.");
            }

            if (!entries.TryGetValue("theme.json", out var manifestEntry)
                || !entries.TryGetValue("theme.css", out var stylesheetEntry))
            {
                throw new ThemePackageValidationException(
                    "Theme archive must contain theme.json and theme.css at its root.");
            }

            if (manifestEntry.Length > MaxManifestBytes)
                throw new ThemePackageValidationException("theme.json exceeds the 32 KiB limit.");
            if (stylesheetEntry.Length > MaxStylesheetBytes)
                throw new ThemePackageValidationException("theme.css exceeds the 1 MiB limit.");

            var manifestJson = await ReadUtf8Async(manifestEntry, MaxManifestBytes, cancellationToken);
            var manifest = ParseManifest(manifestJson);
            ValidateManifestEntries(entries.Keys, manifest);

            Directory.CreateDirectory(destinationPath);
            long extractedBytes = 0;
            foreach (var entry in entries.Values)
            {
                if (entry.FullName.EndsWith('/', StringComparison.Ordinal))
                    continue;

                var filePath = Path.Combine(
                    destinationPath,
                    entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                var fullPath = Path.GetFullPath(filePath);
                if (!IsWithin(destinationPath, fullPath))
                    throw new ThemePackageValidationException("Theme archive entry escapes the package folder.");

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await using var input = entry.Open();
                await using var output = new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous);
                var entryLimit = entry.FullName switch
                {
                    "theme.css" => MaxStylesheetBytes,
                    "theme.json" => MaxManifestBytes,
                    _ => MaxExpandedBytes
                };
                extractedBytes += await CopyBoundedAsync(
                    input,
                    output,
                    Math.Min(entryLimit, MaxExpandedBytes - extractedBytes),
                    cancellationToken);
            }

            await ValidateInstalledAsync(destinationPath, manifest, manifestJson, cancellationToken);
            return new ValidatedThemePackage(manifest, manifestJson);
        }
    }

    public async Task ValidateInstalledAsync(
        string packagePath,
        ThemeManifest manifest,
        string expectedManifestJson,
        CancellationToken cancellationToken = default)
    {
        packagePath = Path.GetFullPath(packagePath);
        if (!Directory.Exists(packagePath))
            throw new ThemePackageValidationException("Theme package files are missing.");
        if ((File.GetAttributes(packagePath) & FileAttributes.ReparsePoint) != 0)
            throw new ThemePackageValidationException("Theme package folder is a reparse point.");

        var manifestPath = Path.Combine(packagePath, "theme.json");
        var stylesheetPath = Path.Combine(packagePath, "theme.css");
        if (!File.Exists(manifestPath) || !File.Exists(stylesheetPath))
            throw new ThemePackageValidationException("Theme package files are incomplete.");

        var actualManifestJson = await File.ReadAllTextAsync(
            manifestPath,
            new UTF8Encoding(false, true),
            cancellationToken);
        if (!string.Equals(actualManifestJson, expectedManifestJson, StringComparison.Ordinal))
            throw new ThemePackageValidationException("The stored theme manifest has been modified.");

        var parsed = ParseManifest(actualManifestJson);
        if (!ManifestsEqual(parsed, manifest))
            throw new ThemePackageValidationException("The stored theme manifest is invalid.");

        var relativeFiles = Directory
            .EnumerateFiles(packagePath, "*", SearchOption.AllDirectories)
            .Select(path =>
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new ThemePackageValidationException("Theme package contains a reparse point.");
                var relative = Path.GetRelativePath(packagePath, path).Replace('\\', '/');
                ValidateArchivePath(relative);
                return relative;
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in Directory.EnumerateDirectories(
                     packagePath,
                     "*",
                     SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                throw new ThemePackageValidationException(
                    "Theme package contains a symbolic link or reparse point.");
        }

        var expected = new HashSet<string>(manifest.Assets, StringComparer.OrdinalIgnoreCase)
        {
            "theme.json",
            "theme.css"
        };
        if (!relativeFiles.SetEquals(expected))
            throw new ThemePackageValidationException("Theme package file set does not match its manifest.");

        await ValidateStylesheetAsync(stylesheetPath, manifest.Assets, cancellationToken);
        foreach (var asset in manifest.Assets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateAssetSignature(Path.Combine(packagePath, asset.Replace('/', Path.DirectorySeparatorChar)), asset);
        }
    }

    public ThemeManifest ParseManifest(string json)
    {
        var bytes = Encoding.UTF8.GetByteCount(json);
        if (bytes > MaxManifestBytes)
            throw new ThemePackageValidationException("theme.json exceeds the 32 KiB limit.");

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new ThemePackageValidationException("theme.json must be a JSON object.");

            var allowed = new HashSet<string>(
                ["name", "slug", "version", "stylesheet", "assets"],
                StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!allowed.Contains(property.Name))
                    throw new ThemePackageValidationException(
                        $"theme.json contains an unsupported property: {property.Name}.");
            }

            var name = GetRequiredString(root, "name");
            var slug = GetRequiredString(root, "slug");
            var version = GetRequiredString(root, "version");
            var stylesheet = GetRequiredString(root, "stylesheet");
            if (!root.TryGetProperty("assets", out var assetsElement)
                || assetsElement.ValueKind != JsonValueKind.Array)
            {
                throw new ThemePackageValidationException("theme.json must contain an assets array.");
            }

            var assets = assetsElement
                .EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String
                    ? item.GetString() ?? ""
                    : throw new ThemePackageValidationException("Manifest assets must be strings."))
                .ToArray();
            var manifest = new ThemeManifest(name, slug, version, stylesheet, assets);
            ValidateManifest(manifest);
            return manifest;
        }
        catch (JsonException ex)
        {
            throw new ThemePackageValidationException($"theme.json is invalid JSON: {ex.Message}");
        }
    }

    public static bool ManifestsEqual(ThemeManifest left, ThemeManifest right) =>
        string.Equals(left.Name, right.Name, StringComparison.Ordinal)
        && string.Equals(left.Slug, right.Slug, StringComparison.Ordinal)
        && string.Equals(left.Version, right.Version, StringComparison.Ordinal)
        && string.Equals(left.Stylesheet, right.Stylesheet, StringComparison.Ordinal)
        && left.Assets.SequenceEqual(right.Assets, StringComparer.OrdinalIgnoreCase);

    private static void ValidateManifest(ThemeManifest manifest)
    {
        if (manifest.Name.Length is < 1 or > 100 || manifest.Name.Any(char.IsControl))
            throw new ThemePackageValidationException("Theme name must contain 1 to 100 visible characters.");
        if (manifest.Slug.Length > 50 || !SlugPattern.IsMatch(manifest.Slug))
            throw new ThemePackageValidationException("Theme slug is invalid.");
        if (manifest.Version.Length > 32 || !VersionPattern.IsMatch(manifest.Version))
            throw new ThemePackageValidationException("Theme version must use major.minor.patch.");
        if (!string.Equals(manifest.Stylesheet, "theme.css", StringComparison.Ordinal))
            throw new ThemePackageValidationException("Theme stylesheet must be theme.css.");
        if (manifest.Assets.Count > MaxEntries - 2)
            throw new ThemePackageValidationException("Theme manifest declares too many assets.");

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in manifest.Assets)
        {
            var path = ValidateArchivePath(asset);
            if (!path.StartsWith("assets/", StringComparison.Ordinal)
                || path.Length > 500
                || !AssetExtensions.Contains(Path.GetExtension(path)))
            {
                throw new ThemePackageValidationException(
                    "Manifest assets must be allow-listed files under assets/.");
            }

            if (!normalized.Add(path))
                throw new ThemePackageValidationException("Manifest contains a duplicate asset.");
        }
    }

    private static void ValidateManifestEntries(
        IEnumerable<string> archivePaths,
        ThemeManifest manifest)
    {
        foreach (var directory in archivePaths.Where(
                     path => path.EndsWith('/', StringComparison.Ordinal)))
        {
            var path = directory.TrimEnd('/');
            if (path != "assets"
                && !path.StartsWith("assets/", StringComparison.Ordinal))
            {
                throw new ThemePackageValidationException(
                    "Theme archive directories must be beneath assets/.");
            }
        }

        var files = archivePaths
            .Where(path => !path.EndsWith('/', StringComparison.Ordinal))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expected = new HashSet<string>(manifest.Assets, StringComparer.OrdinalIgnoreCase)
        {
            "theme.json",
            "theme.css"
        };
        if (!files.SetEquals(expected))
            throw new ThemePackageValidationException(
                "Archive files must exactly match theme.json, theme.css, and manifest assets.");
    }

    private static async Task ValidateStylesheetAsync(
        string path,
        IReadOnlyList<string> assets,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length > MaxStylesheetBytes)
            throw new ThemePackageValidationException("theme.css exceeds the 1 MiB limit.");

        string css;
        try
        {
            css = await File.ReadAllTextAsync(
                path,
                new UTF8Encoding(false, true),
                cancellationToken);
        }
        catch (DecoderFallbackException)
        {
            throw new ThemePackageValidationException("theme.css must be valid UTF-8.");
        }
        var normalizedCss = RemoveCssComments(css);
        var forbidden = new[]
        {
            "@import", "javascript:", "data:", "<style", "</style", "<script", "</script",
            "<html", "expression(", "-moz-binding", "behavior:", "image-set(", "image(",
            "cross-fade(", "src("
        };
        if (forbidden.Any(value =>
                normalizedCss.Contains(value, StringComparison.OrdinalIgnoreCase))
            || normalizedCss.Contains("//", StringComparison.Ordinal)
            || normalizedCss.Contains('\\'))
        {
            throw new ThemePackageValidationException(
                "Theme CSS contains a prohibited executable, remote, or escaped URL construct.");
        }

        var declared = new HashSet<string>(assets, StringComparer.OrdinalIgnoreCase);
        foreach (Match match in CssUrlPattern.Matches(normalizedCss))
        {
            var raw = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
            var url = raw.Trim();
            if (url.StartsWith("/", StringComparison.Ordinal)
                || url.StartsWith("#", StringComparison.Ordinal)
                || url.Contains("://", StringComparison.Ordinal)
                || url.Contains('\\')
                || !declared.Contains(ValidateArchivePath(url)))
            {
                throw new ThemePackageValidationException(
                    "Theme CSS URLs may reference only manifest-declared assets.");
            }
        }
    }

    private static string RemoveCssComments(string css)
    {
        var result = new StringBuilder(css.Length);
        var offset = 0;
        while (offset < css.Length)
        {
            var commentStart = css.IndexOf("/*", offset, StringComparison.Ordinal);
            if (commentStart < 0)
            {
                result.Append(css, offset, css.Length - offset);
                break;
            }

            result.Append(css, offset, commentStart - offset);
            var commentEnd = css.IndexOf("*/", commentStart + 2, StringComparison.Ordinal);
            if (commentEnd < 0)
            {
                throw new ThemePackageValidationException("Theme CSS contains an unterminated comment.");
            }

            offset = commentEnd + 2;
        }

        return result.ToString();
    }

    private static void ValidateAssetSignature(string path, string relativePath)
    {
        if (!File.Exists(path))
            throw new ThemePackageValidationException($"Theme asset is missing: {relativePath}.");

        Span<byte> header = stackalloc byte[64];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var count = stream.Read(header);
        header = header[..count];
        var extension = Path.GetExtension(relativePath);
        var valid = extension.ToLowerInvariant() switch
        {
            ".png" => header.Length >= 8
                && header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => header.Length >= 3
                && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            ".gif" => header.Length >= 6 && (header[..6].SequenceEqual("GIF87a"u8)
                || header[..6].SequenceEqual("GIF89a"u8)),
            ".webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8)
                && header[8..12].SequenceEqual("WEBP"u8),
            ".woff" => header.Length >= 4 && header[..4].SequenceEqual("wOFF"u8),
            ".woff2" => header.Length >= 4 && header[..4].SequenceEqual("wOF2"u8),
            ".avif" => header.Length >= 12 && header[4..8].SequenceEqual("ftyp"u8)
                && (header[8..12].SequenceEqual("avif"u8) || header[8..12].SequenceEqual("avis"u8)),
            _ => false
        };
        if (!valid)
            throw new ThemePackageValidationException(
                $"Theme asset signature does not match its extension: {relativePath}.");
    }

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new ThemePackageValidationException(
                $"theme.json must contain a non-empty {propertyName} string.");
        }

        return value.GetString()!;
    }

    private static async Task<string> ReadUtf8Async(
        ZipArchiveEntry entry,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        if (entry.Length > maximumBytes)
            throw new ThemePackageValidationException("Theme text file is too large.");

        using var memory = new MemoryStream();
        await using var input = entry.Open();
        await CopyBoundedAsync(input, memory, maximumBytes, cancellationToken);
        try
        {
            return new UTF8Encoding(false, true).GetString(memory.ToArray());
        }
        catch (DecoderFallbackException)
        {
            throw new ThemePackageValidationException("Theme manifest must be valid UTF-8.");
        }
    }

    private static async Task<long> CopyBoundedAsync(
        Stream input,
        Stream output,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) != 0)
        {
            total += read;
            if (total > maximumBytes)
                throw new ThemePackageValidationException("Theme content exceeds its size limit.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return total;
    }

    private static string ValidateArchivePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)
            || path.Contains('\\')
            || path.StartsWith("/", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal)
            || path.Contains(':')
            || path.Any(character => character == '\0' || char.IsControl(character)))
        {
            throw new ThemePackageValidationException("Theme archive contains an invalid path.");
        }

        var directoryEntry = path.EndsWith('/', StringComparison.Ordinal);
        var normalized = path.TrimEnd('/');
        if (normalized.Length == 0)
            throw new ThemePackageValidationException("Theme archive contains an invalid path.");

        var segments = normalized.Split('/');
        if (segments.Any(segment =>
                segment is "" or "." or ".."
                || segment.EndsWith(' ')
                || segment.EndsWith('.')
                || IsWindowsDeviceName(segment)))
        {
            throw new ThemePackageValidationException("Theme archive contains an unsafe path.");
        }

        return directoryEntry ? normalized + "/" : normalized;
    }

    private static bool IsWindowsDeviceName(string segment)
    {
        var name = segment.TrimEnd(' ', '.').Split('.')[0].ToUpperInvariant();
        return name is "CON" or "PRN" or "AUX" or "NUL" or "CLOCK$"
            || name.Length == 4 && name.StartsWith("COM", StringComparison.Ordinal)
                && char.IsDigit(name[3]) && name[3] != '0'
            || name.Length == 4 && name.StartsWith("LPT", StringComparison.Ordinal)
                && char.IsDigit(name[3]) && name[3] != '0';
    }

    private static void RejectSymlink(ZipArchiveEntry entry)
    {
        var unixType = (entry.ExternalAttributes >> 16) & 0xF000;
        if (unixType == 0xA000 || entry.FullName.Contains('\\'))
            throw new ThemePackageValidationException("Theme archive contains a link or unsafe path.");
    }

    private static bool IsZipSignature(byte[] signature) =>
        signature[0] == 0x50 && signature[1] == 0x4b
        && (signature[2] is 0x03 or 0x05 or 0x07)
        && signature[3] is 0x04 or 0x06 or 0x08;

    private static bool IsWithin(string root, string path)
    {
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
