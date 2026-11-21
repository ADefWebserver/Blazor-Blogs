using BlazorBlogs.Data;
using BlazorBlogs.Data.Models;

namespace BlazorBlogs.Tests;

public sealed class SocialPreviewBuilderTests
{
    private static readonly Uri BaseUrl = new("https://example.test/");

    [Fact]
    public void BuildPost_NormalizesTitleAndUsesSummaryDescription()
    {
        var preview = Build(
            title: "  A &lt;safe&gt; &amp; useful post  ",
            summary: "<p>Hello&nbsp;&nbsp;world</p><script>unsafe()</script>",
            content: "<p>Ignored content</p>");

        Assert.Equal("A <safe> & useful post", preview.Title);
        Assert.Equal("Hello world", preview.Description);
    }

    [Fact]
    public void BuildPost_FallsBackFromMissingSummaryToPostTextThenSiteName()
    {
        var fromContent = Build(
            summary: " ",
            content: "<p>First</p><p>second&nbsp;paragraph</p>",
            applicationName: "Example");
        var fromSiteName = Build(
            summary: "<script>only script</script>",
            content: "<style>only style</style>",
            applicationName: "Example Blog");

        Assert.Equal("First second paragraph", fromContent.Description);
        Assert.Equal("Example Blog", fromSiteName.Description);
    }

    [Fact]
    public void BuildPost_LimitsDescriptionToTwoHundredUnicodeCodePoints()
    {
        var description = string.Concat(Enumerable.Repeat("😀", 201));

        var preview = Build(summary: description);

        Assert.Equal(200, preview.Description.EnumerateRunes().Count());
        Assert.All(preview.Description.EnumerateRunes(), rune => Assert.Equal("😀", rune.ToString()));
    }

    [Fact]
    public void BuildPost_UsesCanonicalUrlUnderPublicBase()
    {
        var preview = SocialPreviewBuilder.BuildPost(
            new BlogDTO { BlogId = 42, BlogTitle = "Post" },
            new GeneralSettings { ApplicationName = "Example" },
            new Uri("https://example.test/blog/"));

        Assert.Equal(
            "https://example.test/blog/ViewBlogPost/42",
            preview.CanonicalUrl.AbsoluteUri);
    }

    [Fact]
    public void BuildPost_UsesFirstSafePostImage()
    {
        var preview = Build(
            content:
                """
                <img src="javascript:alert(1)">
                <img src="//tracking.example/unsafe.png">
                <img src="images/first.png" alt="A useful image">
                <img src="https://images.example/later.png">
                """);

        Assert.Equal("https://example.test/images/first.png", preview.ImageUrl.AbsoluteUri);
        Assert.Equal("A useful image", preview.ImageAlt);
    }

    [Fact]
    public void BuildPost_AcceptsAbsoluteHttpImage()
    {
        var preview = Build(content: """<img src="http://images.example/hero.png">""");

        Assert.Equal("http://images.example/hero.png", preview.ImageUrl.AbsoluteUri);
    }

    [Fact]
    public void BuildPost_AcceptsRootRelativeImageUnderPublicOrigin()
    {
        var preview = SocialPreviewBuilder.BuildPost(
            new BlogDTO
            {
                BlogId = 7,
                BlogTitle = "Post",
                BlogContent = """<img src="/uploads/hero.png">"""
            },
            new GeneralSettings { ApplicationName = "Example Blog" },
            new Uri("https://example.test/blog/"));

        Assert.Equal("https://example.test/uploads/hero.png", preview.ImageUrl.AbsoluteUri);
    }

    [Theory]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("javascript:alert(1)")]
    [InlineData("//images.example/unsafe.png")]
    [InlineData("#fragment")]
    [InlineData("https://images.example/hero.png#fragment")]
    public void BuildPost_RejectsUnsafePostImageAndUsesLogo(string image)
    {
        var preview = Build(
            content: $"<img src=\"{image}\">",
            applicationLogo: "branding/logo.png");

        Assert.Equal("https://example.test/branding/logo.png", preview.ImageUrl.AbsoluteUri);
        Assert.Equal("Example Blog", preview.ImageAlt);
    }

    [Fact]
    public void BuildPost_UsesRepositoryDefaultWhenNoImageOrLogoIsSafe()
    {
        var preview = Build(
            content: """<img src="javascript:alert(1)">""",
            applicationLogo: "data:image/png;base64,AAAA");

        Assert.Equal("https://example.test/images/social-preview.png", preview.ImageUrl.AbsoluteUri);
        Assert.Equal("summary_large_image", preview.TwitterCard);
    }

    [Fact]
    public void NormalizeBaseUrl_UsesConfiguredUrlAndRemovesQueryAndFragment()
    {
        var result = SiteUrlService.NormalizeBaseUrl(
            "https://public.example/blog?preview=1#fragment",
            "https://request.example/internal/");

        Assert.Equal("https://public.example/blog/", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relative")]
    public void NormalizeBaseUrl_UsesRequestFallbackForInvalidConfiguration(
        string configured)
    {
        Assert.Equal(
            "https://request.example/blog/",
            SiteUrlService.NormalizeBaseUrl(configured, "https://request.example/blog"));
    }

    [Fact]
    public void ViewBlogPost_DeclaresCompleteMetadata()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "BlazorBlogs",
            "Components",
            "Pages",
            "ViewBlogPost.razor");
        var source = File.ReadAllText(path);

        foreach (var metadataName in new[]
        {
            "description",
            "og:title",
            "og:description",
            "og:url",
            "og:type",
            "og:site_name",
            "og:image",
            "twitter:card",
            "twitter:title",
            "twitter:description",
            "twitter:image"
        })
        {
            Assert.Contains(metadataName, source, StringComparison.Ordinal);
        }

        Assert.Contains("Rel=\"canonical\"", source, StringComparison.Ordinal);
        Assert.Contains("Status404NotFound", source, StringComparison.Ordinal);
    }

    private static SocialPreview Build(
        string title = "Post",
        string summary = "",
        string content = "",
        string applicationName = "Example Blog",
        string applicationLogo = "")
    {
        return SocialPreviewBuilder.BuildPost(
            new BlogDTO
            {
                BlogId = 7,
                BlogTitle = title,
                BlogSummary = summary,
                BlogContent = content
            },
            new GeneralSettings
            {
                ApplicationName = applicationName,
                ApplicationLogo = applicationLogo
            },
            BaseUrl);
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

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
