using BlazorBlogs.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace BlazorBlogs;

public sealed class ExternalAuthOptionsStore(
    IOptionsMonitorCache<GoogleOptions> optionsCache)
    : IPostConfigureOptions<GoogleOptions>
{
    public const string GoogleEmailVerifiedClaim = "urn:blazorblogs:google:email_verified";
    public const string PlaceholderClientId = "google-client-id-not-configured";
    public const string PlaceholderClientSecret = "google-client-secret-not-configured";

    private readonly object sync = new();
    private GoogleAuthSettings settings = new();

    public bool IsGoogleConfigured
    {
        get
        {
            lock (sync)
            {
                return settings.IsFullyConfigured;
            }
        }
    }

    public void UpdateGoogle(GoogleAuthSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (sync)
        {
            this.settings = new GoogleAuthSettings
            {
                Enabled = settings.Enabled,
                ClientId = settings.ClientId?.Trim() ?? "",
                ClientSecret = settings.ClientSecret?.Trim() ?? ""
            };
        }

        optionsCache.TryRemove(GoogleDefaults.AuthenticationScheme);
    }

    public void PostConfigure(string name, GoogleOptions options)
    {
        if (!string.Equals(name, GoogleDefaults.AuthenticationScheme, StringComparison.Ordinal))
            return;

        GoogleAuthSettings configured;
        lock (sync)
        {
            configured = new GoogleAuthSettings
            {
                Enabled = settings.Enabled,
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret
            };
        }

        options.ClientId = configured.IsFullyConfigured
            ? configured.ClientId
            : PlaceholderClientId;
        options.ClientSecret = configured.IsFullyConfigured
            ? configured.ClientSecret
            : PlaceholderClientSecret;
        options.SaveTokens = false;
        // Google's v2 userinfo endpoint returns "verified_email"; the v3 / OpenID Connect
        // endpoint returns "email_verified". Map whichever one is present.
        options.ClaimActions.Remove(GoogleEmailVerifiedClaim);
        options.ClaimActions.MapJsonKey(GoogleEmailVerifiedClaim, "verified_email");
        options.ClaimActions.MapJsonKey(GoogleEmailVerifiedClaim, "email_verified");
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(
                QueryHelpers.AddQueryString(context.RedirectUri, "prompt", "login"));
            return Task.CompletedTask;
        };
    }
}
