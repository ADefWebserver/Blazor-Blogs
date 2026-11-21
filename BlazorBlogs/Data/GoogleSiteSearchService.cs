using System.Text.RegularExpressions;

namespace BlazorBlogs.Data;

public sealed record GoogleSiteSearchQuery(string Terms, string Site);

public interface IGoogleSiteSearchService
{
    GoogleSiteSearchQuery BuildQuery(string terms, Uri baseUrl);
    Uri BuildDestination(GoogleSiteSearchQuery query);
}

public sealed class GoogleSiteSearchService : IGoogleSiteSearchService
{
    private const string SearchEndpoint = "https://www.google.com/search";
    private static readonly Regex SiteOperator = new(
        @"(?<![\p{L}\p{N}_])site\s*:\s*(?:""[^""]*""|'[^']*'|[^\s]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public GoogleSiteSearchQuery BuildQuery(string terms, Uri baseUrl)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(baseUrl);

        var safeTerms = RemoveSiteOperators(terms);
        if (string.IsNullOrWhiteSpace(safeTerms))
        {
            throw new ArgumentException("Search terms cannot be blank.", nameof(terms));
        }

        return new GoogleSiteSearchQuery(safeTerms.Trim(), GetSiteRestriction(baseUrl));
    }

    public Uri BuildDestination(GoogleSiteSearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var terms = RemoveSiteOperators(query.Terms);
        if (string.IsNullOrWhiteSpace(terms))
        {
            throw new ArgumentException("Search terms cannot be blank.", nameof(query));
        }

        var site = NormalizeSiteRestriction(query.Site);
        var encodedQuery = Uri.EscapeDataString($"{terms.Trim()} site:{site}");
        return new Uri($"{SearchEndpoint}?q={encodedQuery}", UriKind.Absolute);
    }

    private static string RemoveSiteOperators(string terms) =>
        SiteOperator.Replace(terms, string.Empty);

    private static string GetSiteRestriction(Uri baseUrl)
    {
        if (!baseUrl.IsAbsoluteUri ||
            (!string.Equals(baseUrl.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(baseUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
            !string.IsNullOrEmpty(baseUrl.UserInfo))
        {
            throw new ArgumentException("A valid HTTP or HTTPS base URL is required.", nameof(baseUrl));
        }

        var host = baseUrl.IdnHost;
        if (baseUrl.HostNameType == UriHostNameType.IPv6)
        {
            host = $"[{host.Trim('[', ']')}]";
        }

        return baseUrl.IsDefaultPort ? host : $"{host}:{baseUrl.Port}";
    }

    private static string NormalizeSiteRestriction(string site)
    {
        if (string.IsNullOrWhiteSpace(site) || site.Any(char.IsWhiteSpace) ||
            !Uri.TryCreate($"https://{site}/", UriKind.Absolute, out var parsedSite) ||
            parsedSite.PathAndQuery != "/" ||
            !string.IsNullOrEmpty(parsedSite.Fragment) ||
            !string.IsNullOrEmpty(parsedSite.UserInfo))
        {
            throw new ArgumentException("A valid site restriction is required.", nameof(site));
        }

        return GetSiteRestriction(parsedSite);
    }
}
