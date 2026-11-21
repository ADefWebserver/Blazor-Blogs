using System.Text;
using BlazorBlogs.Data.Models;
using HtmlAgilityPack;

namespace BlazorBlogs.Data;

public interface ISocialPreviewBuilder
{
    Task<SocialPreview> BuildPostAsync(
        BlogDTO post,
        Uri fallbackBaseUrl,
        CancellationToken cancellationToken = default);

    Task<SocialPreview> BuildNotFoundAsync(
        int? postId,
        Uri fallbackBaseUrl,
        CancellationToken cancellationToken = default);
}

public sealed class SocialPreviewBuilder(
    GeneralSettingsService settingsService,
    SiteUrlService siteUrlService) : ISocialPreviewBuilder
{
    public const int MaximumDescriptionLength = 200;

    public async Task<SocialPreview> BuildPostAsync(
        BlogDTO post,
        Uri fallbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(post);

        var settings = await settingsService.GetGeneralSettingsAsync();
        var baseUrl = await GetBaseUrlAsync(fallbackBaseUrl);

        return BuildPost(post, settings, baseUrl);
    }

    public async Task<SocialPreview> BuildNotFoundAsync(
        int? postId,
        Uri fallbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = await settingsService.GetGeneralSettingsAsync();
        var baseUrl = await GetBaseUrlAsync(fallbackBaseUrl);
        var siteName = NormalizeText(settings.ApplicationName);

        if (string.IsNullOrEmpty(siteName))
        {
            siteName = "Blazor-Blogs";
        }

        var canonicalUrl = postId > 0
            ? new Uri(baseUrl, $"ViewBlogPost/{postId}")
            : baseUrl;

        return new SocialPreview
        {
            Title = "Post not found",
            Description = "The requested blog post could not be found.",
            CanonicalUrl = canonicalUrl,
            SiteName = siteName,
            ImageUrl = new Uri(baseUrl, "images/social-preview.png"),
            ImageAlt = siteName
        };
    }

    public static SocialPreview BuildPost(
        BlogDTO post,
        GeneralSettings settings,
        Uri baseUrl)
    {
        ArgumentNullException.ThrowIfNull(post);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(baseUrl);

        if (!IsHttpUrl(baseUrl))
        {
            throw new ArgumentException("The social preview base URL must use HTTP or HTTPS.", nameof(baseUrl));
        }

        var siteName = NormalizeText(settings.ApplicationName);
        if (string.IsNullOrEmpty(siteName))
        {
            siteName = "Blazor-Blogs";
        }

        var title = NormalizeText(post.BlogTitle);
        if (string.IsNullOrEmpty(title))
        {
            title = "Blog post";
        }

        var description = NormalizeText(post.BlogSummary);
        if (string.IsNullOrEmpty(description))
        {
            description = NormalizeText(post.BlogContent);
        }

        if (string.IsNullOrEmpty(description))
        {
            description = siteName;
        }

        description = LimitCodePoints(description, MaximumDescriptionLength);

        var selectedImage = FindFirstSafeImage(post.BlogContent, baseUrl);
        var imageUrl = selectedImage?.Url;
        var imageAlt = string.IsNullOrEmpty(selectedImage?.Alt)
            ? (selectedImage is null ? null : title)
            : selectedImage.Alt;

        if (imageUrl is null &&
            TryResolveImageUrl(settings.ApplicationLogo, baseUrl, out var logoUrl))
        {
            imageUrl = logoUrl;
            imageAlt = siteName;
        }

        if (imageUrl is null)
        {
            imageUrl = new Uri(baseUrl, "images/social-preview.png");
            imageAlt = siteName;
        }

        return new SocialPreview
        {
            Title = title,
            Description = description,
            CanonicalUrl = new Uri(baseUrl, $"ViewBlogPost/{post.BlogId}"),
            SiteName = siteName,
            ImageUrl = imageUrl,
            ImageAlt = imageAlt
        };
    }

    public static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var document = new HtmlDocument();
        document.LoadHtml(text);

        var extracted = new StringBuilder();
        AppendText(document.DocumentNode, extracted);
        var decoded = HtmlEntity.DeEntitize(extracted.ToString());
        var normalized = new StringBuilder(decoded.Length);
        var needsSpace = false;

        foreach (var rune in decoded.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                if (normalized.Length > 0)
                {
                    needsSpace = true;
                }

                continue;
            }

            if (needsSpace)
            {
                normalized.Append(' ');
                needsSpace = false;
            }

            normalized.Append(rune);
        }

        return normalized.ToString();
    }

    private async Task<Uri> GetBaseUrlAsync(Uri fallbackBaseUrl)
    {
        var baseUrl = await siteUrlService.GetBaseUrlAsync(fallbackBaseUrl.AbsoluteUri);
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var absolute) ||
            !IsHttpUrl(absolute))
        {
            throw new InvalidOperationException("A valid HTTP or HTTPS base URL is required for social previews.");
        }

        return absolute;
    }

    private static SafeImage FindFirstSafeImage(string html, Uri baseUrl)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        var images = document.DocumentNode.SelectNodes("//img[@src]");
        if (images is null)
        {
            return null;
        }

        foreach (var image in images)
        {
            var source = HtmlEntity.DeEntitize(image.GetAttributeValue("src", string.Empty));
            if (TryResolveImageUrl(source, baseUrl, out var imageUrl))
            {
                var imageAlt = NormalizeText(image.GetAttributeValue("alt", string.Empty));
                return new SafeImage(imageUrl, imageAlt);
            }
        }

        return null;
    }

    private static bool TryResolveImageUrl(string value, Uri baseUrl, out Uri imageUrl)
    {
        imageUrl = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var source = value.Trim();
        if (source.StartsWith("//", StringComparison.Ordinal) ||
            source.StartsWith("\\\\", StringComparison.Ordinal) ||
            source.StartsWith('#') ||
            source.Contains('\\'))
        {
            return false;
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out var absolute))
        {
            if (!IsHttpUrl(absolute) || !string.IsNullOrEmpty(absolute.Fragment))
            {
                return false;
            }

            imageUrl = absolute;
            return true;
        }

        var isRootRelative = source.StartsWith('/', StringComparison.Ordinal);
        if (source.Contains(':') ||
            !Uri.TryCreate(baseUrl, source, out var relative) ||
            !IsHttpUrl(relative) ||
            !string.IsNullOrEmpty(relative.Fragment) ||
            (!isRootRelative && !IsUnderBasePath(baseUrl, relative)))
        {
            return false;
        }

        if (!string.Equals(baseUrl.Scheme, relative.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(baseUrl.IdnHost, relative.IdnHost, StringComparison.OrdinalIgnoreCase) ||
            baseUrl.Port != relative.Port)
        {
            return false;
        }

        imageUrl = relative;
        return true;
    }

    private static bool IsUnderBasePath(Uri baseUrl, Uri candidate)
    {
        if (!string.Equals(baseUrl.Scheme, candidate.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(baseUrl.IdnHost, candidate.IdnHost, StringComparison.OrdinalIgnoreCase) ||
            baseUrl.Port != candidate.Port)
        {
            return false;
        }

        var basePath = baseUrl.AbsolutePath.TrimEnd('/') + "/";
        return basePath == "/" ||
            candidate.AbsolutePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHttpUrl(Uri uri) =>
        uri.IsAbsoluteUri &&
        (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));

    private static void AppendText(HtmlNode node, StringBuilder result)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            result.Append(node.InnerText);
            return;
        }

        if (node.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var separatesText =
            node.Name.Equals("br", StringComparison.OrdinalIgnoreCase) ||
            node.Name is "p" or "div" or "li" or "blockquote" or "pre" or
                "article" or "section" or "header" or "footer" or
                "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or
                "ul" or "ol" or "tr" or "td" or "th";

        if (separatesText)
        {
            result.Append(' ');
        }

        foreach (var child in node.ChildNodes)
        {
            AppendText(child, result);
        }

        if (separatesText)
        {
            result.Append(' ');
        }
    }

    private static string LimitCodePoints(string text, int maximumLength)
    {
        var result = new StringBuilder();
        var count = 0;

        foreach (var rune in text.EnumerateRunes())
        {
            if (count == maximumLength)
            {
                break;
            }

            result.Append(rune);
            count++;
        }

        return result.ToString();
    }

    private sealed record SafeImage(Uri Url, string Alt);
}
