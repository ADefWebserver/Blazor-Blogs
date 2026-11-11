using BlazorBlogs.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorBlogs.Data;

public sealed class GoogleAuthSettingsService(BlazorBlogsContext context)
{
    public async Task<GoogleAuthSettings> GetAsync()
    {
        var values = await context.Settings
            .AsNoTracking()
            .Where(x => SettingNames.Contains(x.SettingName))
            .ToDictionaryAsync(
                x => x.SettingName,
                x => x.SettingValue ?? "",
                StringComparer.OrdinalIgnoreCase);

        return new GoogleAuthSettings
        {
            Enabled = bool.TryParse(Get(values, EnabledSetting), out var enabled) && enabled,
            ClientId = Get(values, ClientIdSetting),
            ClientSecret = Get(values, ClientSecretSetting)
        };
    }

    public async Task SaveAsync(GoogleAuthSettings settings)
    {
        var existing = await GetAsync();
        await SetAsync(EnabledSetting, settings.Enabled.ToString());
        await SetAsync(ClientIdSetting, settings.ClientId?.Trim() ?? "");
        await SetAsync(
            ClientSecretSetting,
            string.IsNullOrWhiteSpace(settings.ClientSecret)
                ? existing.ClientSecret
                : settings.ClientSecret.Trim());
        await context.SaveChangesAsync();
    }

    private async Task SetAsync(string name, string value)
    {
        var setting = await context.Settings.FirstOrDefaultAsync(x => x.SettingName == name);
        if (setting is null)
        {
            setting = new Settings { SettingName = name };
            context.Settings.Add(setting);
        }

        setting.SettingValue = value;
    }

    private static string Get(
        IReadOnlyDictionary<string, string> values,
        string name) =>
        values.TryGetValue(name, out var value) ? value : "";

    public const string EnabledSetting = "Authentication:Google:Enabled";
    public const string ClientIdSetting = "Authentication:Google:ClientId";
    public const string ClientSecretSetting = "Authentication:Google:ClientSecret";

    private static readonly HashSet<string> SettingNames = new(StringComparer.OrdinalIgnoreCase)
    {
        EnabledSetting,
        ClientIdSetting,
        ClientSecretSetting
    };
}
