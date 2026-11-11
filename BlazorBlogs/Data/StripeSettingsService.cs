using BlazorBlogs.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorBlogs.Data;

public sealed class StripeSettings
{
    public bool Enabled { get; set; }
    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public string Currency { get; set; } = "usd";
    public bool AutomaticTax { get; set; } = true;
    public string TaxBehavior { get; set; } = "exclusive";
    public string TaxCode { get; set; } = "";
    public int GuestLinkDays { get; set; } = 7;
    public int GuestMaxDownloads { get; set; } = 5;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey) && !string.IsNullOrWhiteSpace(WebhookSecret);
}

public sealed class StripeSettingsService(BlazorBlogsContext context)
{
    public async Task<StripeSettings> GetAsync()
    {
        var values = await context.Settings.AsNoTracking()
            .Where(x => StripeSettingNames.Contains(x.SettingName))
            .ToDictionaryAsync(x => x.SettingName, x => x.SettingValue ?? "", StringComparer.OrdinalIgnoreCase);

        string Get(string key, string fallback = "") => values.TryGetValue(key, out var value) ? value : fallback;
        bool Bool(string key, bool fallback) => bool.TryParse(Get(key), out var value) ? value : fallback;
        int Number(string key, int fallback) => int.TryParse(Get(key), out var value) ? value : fallback;

        return new StripeSettings
        {
            Enabled = Bool("StripeEnabled", false),
            SecretKey = Get("StripeSecretKey"),
            WebhookSecret = Get("StripeWebhookSecret"),
            Currency = Get("StripeCurrency", "usd").Trim().ToLowerInvariant(),
            AutomaticTax = Bool("StripeAutomaticTax", true),
            TaxBehavior = Get("StripeTaxBehavior", "exclusive").Trim().ToLowerInvariant(),
            TaxCode = Get("StripeTaxCode"),
            GuestLinkDays = Number("StripeGuestLinkDays", 7),
            GuestMaxDownloads = Number("StripeGuestMaxDownloads", 5)
        };
    }

    public async Task SaveAsync(StripeSettings settings)
    {
        var currency = settings.Currency?.Trim().ToLowerInvariant();
        if (!StripeAmount.IsSupported(currency))
            throw new InvalidOperationException("The Stripe currency must be a supported two-decimal or zero-decimal three-letter currency.");
        if (settings.GuestLinkDays is < 1 or > 365)
            throw new InvalidOperationException("Guest link days must be between 1 and 365.");
        if (settings.GuestMaxDownloads is < 1 or > 100)
            throw new InvalidOperationException("Guest maximum downloads must be between 1 and 100.");
        if (settings.TaxBehavior is not ("exclusive" or "inclusive"))
            throw new InvalidOperationException("Tax behaviour must be exclusive or inclusive.");

        var existing = await GetAsync();
        await SetAsync("StripeEnabled", settings.Enabled.ToString());
        await SetAsync("StripeSecretKey", string.IsNullOrEmpty(settings.SecretKey) ? existing.SecretKey : settings.SecretKey.Trim());
        await SetAsync("StripeWebhookSecret", string.IsNullOrEmpty(settings.WebhookSecret) ? existing.WebhookSecret : settings.WebhookSecret.Trim());
        await SetAsync("StripeCurrency", currency);
        await SetAsync("StripeAutomaticTax", settings.AutomaticTax.ToString());
        await SetAsync("StripeTaxBehavior", settings.TaxBehavior);
        await SetAsync("StripeTaxCode", settings.TaxCode?.Trim() ?? "");
        await SetAsync("StripeGuestLinkDays", settings.GuestLinkDays.ToString());
        await SetAsync("StripeGuestMaxDownloads", settings.GuestMaxDownloads.ToString());
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
        setting.SettingValue = value ?? "";
    }

    private static readonly HashSet<string> StripeSettingNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "StripeEnabled", "StripeSecretKey", "StripeWebhookSecret", "StripeCurrency",
        "StripeAutomaticTax", "StripeTaxBehavior", "StripeTaxCode",
        "StripeGuestLinkDays", "StripeGuestMaxDownloads"
    };
}
