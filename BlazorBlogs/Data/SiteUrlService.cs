namespace BlazorBlogs.Data;

public sealed class SiteUrlService(GeneralSettingsService settingsService)
{
    public async Task<string> GetBaseUrlAsync(string fallbackBaseUrl)
    {
        var configuredUrl = await settingsService.GetSettingAsync("SiteBaseUrl");
        var baseUrl = string.IsNullOrWhiteSpace(configuredUrl)
            ? fallbackBaseUrl
            : configuredUrl;

        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var absolute)
            ? absolute.AbsoluteUri.TrimEnd('/') + "/"
            : fallbackBaseUrl.TrimEnd('/') + "/";
    }
}
