using System.IO.Compression;
using System.Text;
using BlazorBlogs.Data;

namespace BlazorBlogs.Tests;

public sealed class ThemePackageValidatorTests
{
    [Fact]
    public async Task ValidPackage_ExtractsAndValidates()
    {
        var root = TestRoot.Create();
        try
        {
            await using var archive = CreateArchive(
                Manifest(),
                "body { background: url('assets/example.png'); }",
                ("assets/example.png", Png()));

            var result = await new ThemePackageValidator().ExtractAsync(
                archive,
                Path.Combine(root.Path, "installed"));

            Assert.Equal("example-theme", result.Manifest.Slug);
            Assert.True(File.Exists(Path.Combine(root.Path, "installed", "theme.css")));
            Assert.True(File.Exists(Path.Combine(root.Path, "installed", "assets", "example.png")));
        }
        finally
        {
            root.Dispose();
        }
    }

    [Theory]
    [InlineData("../theme.css")]
    [InlineData("/theme.css")]
    [InlineData("C:/theme.css")]
    [InlineData(@"assets\example.png")]
    [InlineData("assets/CON.png")]
    [InlineData("assets/..")]
    public async Task UnsafeArchivePath_IsRejected(string path)
    {
        var root = TestRoot.Create();
        try
        {
            await using var archive = CreateArchive(
                Manifest("[]"),
                "body { color: black; }",
                (path, Png()));

            await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                new ThemePackageValidator().ExtractAsync(
                    archive,
                    Path.Combine(root.Path, "installed")));
        }
        finally
        {
            root.Dispose();
        }
    }

    [Fact]
    public async Task DuplicateNormalizedPath_IsRejected()
    {
        var root = TestRoot.Create();
        try
        {
            await using var archive = CreateRawArchive(
                ("theme.json", Encoding.UTF8.GetBytes(Manifest()), null),
                ("theme.css", Encoding.UTF8.GetBytes("body{}"), null),
                ("theme.css", Encoding.UTF8.GetBytes("html{}"), null));

            await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                new ThemePackageValidator().ExtractAsync(
                    archive,
                    Path.Combine(root.Path, "installed")));
        }
        finally
        {
            root.Dispose();
        }
    }

    [Fact]
    public async Task SymlinkEntry_IsRejected()
    {
        var root = TestRoot.Create();
        try
        {
            await using var archive = CreateRawArchive(
                ("theme.json", Encoding.UTF8.GetBytes(Manifest()), null),
                ("theme.css", Encoding.UTF8.GetBytes("body{}"), null),
                ("assets/link.png", Png(), unchecked((int)((0xA000 | 0x1ff) << 16))));

            await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                new ThemePackageValidator().ExtractAsync(
                    archive,
                    Path.Combine(root.Path, "installed")));
        }
        finally
        {
            root.Dispose();
        }
    }

    [Theory]
    [InlineData("@import url('assets/example.png');")]
    [InlineData("body{background:url(https://example.invalid/example.png)}")]
    [InlineData("body{background:url(//example.invalid/example.png)}")]
    [InlineData("body{background:url(data:image/png;base64,AAAA)}")]
    [InlineData("body{background:url(javascript:alert(1))}")]
    [InlineData("body{background:url(assets/not-declared.png)}")]
    [InlineData("<style>body{}</style>")]
    public async Task UnsafeCss_IsRejected(string css)
    {
        var root = TestRoot.Create();
        try
        {
            await using var archive = CreateArchive(
                Manifest(),
                css,
                ("assets/example.png", Png()));

            await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                new ThemePackageValidator().ExtractAsync(
                    archive,
                    Path.Combine(root.Path, "installed")));
        }
        finally
        {
            root.Dispose();
        }
    }

    [Theory]
    [InlineData("@im/**/port url('assets/example.png');")]
    [InlineData("body{background:u/**/rl(https://example.invalid/example.png)}")]
    public async Task CommentObfuscatedCss_IsRejected(string css)
    {
        var root = TestRoot.Create();
        try
        {
            await using var archive = CreateArchive(
                Manifest(),
                css,
                ("assets/example.png", Png()));

            await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                new ThemePackageValidator().ExtractAsync(
                    archive,
                    Path.Combine(root.Path, "installed")));
        }
        finally
        {
            root.Dispose();
        }
    }

    [Theory]
    [InlineData("Bad_Slug")]
    [InlineData("v1")]
    [InlineData("1.2")]
    [InlineData("01.2.3")]
    public async Task InvalidManifestSlugOrVersion_IsRejected(string slugOrVersion)
    {
        var root = TestRoot.Create();
        try
        {
            var manifest = slugOrVersion == "Bad_Slug"
                ? Manifest(slug: "Bad_Slug")
                : Manifest(version: slugOrVersion);
            await using var archive = CreateArchive(
                manifest,
                "body{}");

            await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                new ThemePackageValidator().ExtractAsync(
                    archive,
                    Path.Combine(root.Path, "installed")));
        }
        finally
        {
            root.Dispose();
        }
    }

    [Fact]
    public async Task InvalidUtf8Manifest_IsRejected()
    {
        var root = TestRoot.Create();
        try
        {
            await using var archive = CreateRawArchive(
                ("theme.json", new byte[] { 0xc3, 0x28 }),
                ("theme.css", Encoding.UTF8.GetBytes("body{}")));

            await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                new ThemePackageValidator().ExtractAsync(
                    archive,
                    Path.Combine(root.Path, "installed")));
        }
        finally
        {
            root.Dispose();
        }
    }

    [Fact]
    public async Task AssetExtensionSpoof_IsRejected()
    {
        var root = TestRoot.Create();
        try
        {
            await using var archive = CreateArchive(
                Manifest(),
                "body{}",
                ("assets/example.png", Encoding.ASCII.GetBytes("<script>alert(1)</script>")));

            await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                new ThemePackageValidator().ExtractAsync(
                    archive,
                    Path.Combine(root.Path, "installed")));
        }
        finally
        {
            root.Dispose();
        }
    }

    [Fact]
    public async Task DisallowedAssetExtension_IsRejected()
        {
            var root = TestRoot.Create();
            try
            {
                await using var archive = CreateArchive(
                    Manifest("[\"assets/example.svg\"]"),
                    "body{}",
                    ("assets/example.svg", Encoding.UTF8.GetBytes("<svg></svg>")));

                await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                    new ThemePackageValidator().ExtractAsync(
                        archive,
                        Path.Combine(root.Path, "installed")));
            }
            finally
            {
                root.Dispose();
            }
        }

    [Fact]
    public async Task EntryLimit_IsRejected()
        {
            var root = TestRoot.Create();
            try
            {
                var entries = new List<(string, byte[], int?)>
                {
                    ("theme.json", Encoding.UTF8.GetBytes(Manifest("[]")), null),
                    ("theme.css", Encoding.UTF8.GetBytes("body{}"), null)
                };
                entries.AddRange(Enumerable.Range(0, ThemePackageValidator.MaxEntries)
                    .Select(index => ($"assets/{index}.png", Png(), (int?)null)));
                await using var archive = CreateRawArchive(entries.ToArray());

                await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                    new ThemePackageValidator().ExtractAsync(
                        archive,
                        Path.Combine(root.Path, "installed")));
            }
            finally
            {
                root.Dispose();
            }
        }

    [Fact]
    public async Task StylesheetAndManifestLimits_AreRejected()
        {
            var validator = new ThemePackageValidator();
            var root = TestRoot.Create();
            try
            {
                await using (var stylesheetArchive = CreateRawArchive(
                    ("theme.json", Encoding.UTF8.GetBytes(Manifest("[]")), null),
                    ("theme.css", new byte[ThemePackageValidator.MaxStylesheetBytes + 1], null)))
                {
                    await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                        validator.ExtractAsync(
                            stylesheetArchive,
                            Path.Combine(root.Path, "large-css")));
                }

                await using (var manifestArchive = CreateRawArchive(
                    ("theme.json", new byte[ThemePackageValidator.MaxManifestBytes + 1], null),
                    ("theme.css", Encoding.UTF8.GetBytes("body{}"), null)))
                {
                    await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                        validator.ExtractAsync(
                            manifestArchive,
                            Path.Combine(root.Path, "large-manifest")));
                }
            }
            finally
            {
                root.Dispose();
            }
        }
    [Fact]
    public async Task ExpandedLimit_IsRejected()
    {
        var root = TestRoot.Create();
        try
        {
            var oversized = new byte[20 * 1024 * 1024 + 1];
            await using var archive = CreateRawArchive(
                ("theme.json", Encoding.UTF8.GetBytes(Manifest())),
                ("theme.css", Encoding.UTF8.GetBytes("body{}")),
                ("assets/large.bin", oversized));

            await Assert.ThrowsAsync<ThemePackageValidationException>(() =>
                new ThemePackageValidator().ExtractAsync(
                    archive,
                    Path.Combine(root.Path, "installed")));
        }
        finally
        {
            root.Dispose();
        }
    }

    private static MemoryStream CreateArchive(
        string manifest,
        string css,
        params (string Path, byte[] Content)[] assets)
    {
        var entries = new List<(string, byte[], int?)>
        {
            ("theme.json", Encoding.UTF8.GetBytes(manifest), null),
            ("theme.css", Encoding.UTF8.GetBytes(css), null)
        };
        entries.AddRange(assets.Select(asset => (asset.Path, asset.Content, (int?)null)));
        return CreateRawArchive(entries.ToArray());
    }

    private static MemoryStream CreateRawArchive(
        params (string Path, byte[], int?)[] entries)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content, attributes) in entries)
            {
                var entry = zip.CreateEntry(path);
                if (attributes is not null)
                    entry.ExternalAttributes = attributes.Value;
                using var output = entry.Open();
                output.Write(content);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateRawArchive(params (string Path, byte[])[] entries) =>
        CreateRawArchive(entries.Select(item => (item.Path, item.Item2, (int?)null)).ToArray());

    private static string Manifest(
        string assets = "[\"assets/example.png\"]",
        string slug = "example-theme",
        string version = "1.0.0") =>
        $$"""
        {
          "name": "Example Theme",
          "slug": "{{slug}}",
          "version": "{{version}}",
          "stylesheet": "theme.css",
          "assets": {{assets}}
        }
        """;

    private static byte[] Png() =>
        [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0];
}

internal sealed class TestRoot : IDisposable
{
    private TestRoot()
    {
        Path = System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "theme-package-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }
    public static TestRoot Create() => new();

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}
