namespace BlazorBlogs.Data;

public sealed class SiteUrlService(GeneralSettingsService settingsService)
{
    public async Task<string> GetBaseUrlAsync(string fallbackBaseUrl)
    {
        var configuredUrl = await settingsService.GetSettingAsync("SiteBaseUrl");
        return NormalizeBaseUrl(configuredUrl, fallbackBaseUrl);
    }

    public static string NormalizeBaseUrl(string configuredBaseUrl, string fallbackBaseUrl)
    {
        if (TryNormalizeHttpUrl(configuredBaseUrl, out var configured))
        {
            return configured;
        }

        if (TryNormalizeHttpUrl(fallbackBaseUrl, out var fallback))
        {
            return fallback;
        }

        throw new UriFormatException("A valid HTTP or HTTPS base URL is required.");
    }

    private static bool TryNormalizeHttpUrl(string value, out string normalized)
    {
        normalized = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var absolute) ||
            (!string.Equals(absolute.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(absolute.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var builder = new UriBuilder(absolute)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        builder.Path = builder.Path.TrimEnd('/') + "/";
        normalized = builder.Uri.AbsoluteUri;
        return true;
    }
}
