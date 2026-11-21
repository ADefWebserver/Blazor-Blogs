using BlazorBlogs.Data;
using BlazorBlogs.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorBlogs.Tests;

public sealed class GoogleSiteSearchServiceTests
{
    private readonly GoogleSiteSearchService service = new();

    [Fact]
    public void BuildDestination_EncodesTheCompleteQueryExactlyOnce()
    {
        var query = service.BuildQuery(
            "space + & # / \" café 中文",
            new Uri("https://public.example/blog/"));

        var destination = service.BuildDestination(query);

        Assert.Equal(
            "https://www.google.com/search?q=space%20%2B%20%26%20%23%20%2F%20%22%20caf%C3%A9%20%E4%B8%AD%E6%96%87%20site%3Apublic.example",
            destination.AbsoluteUri);
    }

    [Fact]
    public void BuildQuery_UsesOnlyTheTrustedBaseAuthorityAndDoesNotMutateTerms()
    {
        var terms = "alpha site:untrusted.example beta SITE : ignored.example";
        var original = terms;

        var query = service.BuildQuery(
            terms,
            new Uri("https://public.example:8443/blog?preview=true#fragment"));
        var decodedQuery = Uri.UnescapeDataString(
            service.BuildDestination(query).Query.Substring("?q=".Length));

        Assert.Equal(original, terms);
        Assert.Equal("public.example:8443", query.Site);
        Assert.Equal(1, CountOccurrences(decodedQuery, "site:"));
        Assert.EndsWith("site:public.example:8443", decodedQuery);
        Assert.DoesNotContain("untrusted.example", decodedQuery);
        Assert.DoesNotContain("site : ignored", decodedQuery, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildQuery_RejectsBlankTermsAndUnsafeBaseUrls()
    {
        Assert.Throws<ArgumentException>(
            () => service.BuildQuery(" ", new Uri("https://example.test/")));
        Assert.Throws<ArgumentException>(
            () => service.BuildQuery("terms", new Uri("javascript:alert(1)")));
    }

    [Fact]
    public void BuildQuery_UsesConfiguredOrFallbackOrigin()
    {
        var configured = SiteUrlService.NormalizeBaseUrl(
            "https://configured.example/path/",
            "https://request.example/");
        var fallback = SiteUrlService.NormalizeBaseUrl(
            "not-a-url",
            "https://request.example/blog/");

        Assert.Equal(
            "configured.example",
            service.BuildQuery("terms", new Uri(configured)).Site);
        Assert.Equal(
            "request.example",
            service.BuildQuery("terms", new Uri(fallback)).Site);
    }

    [Fact]
    public void BuildDestination_HandlesLongTerms()
    {
        var terms = string.Concat(Enumerable.Repeat("search ", 1000));
        var query = service.BuildQuery(terms, new Uri("https://example.test/"));

        var destination = service.BuildDestination(query);

        Assert.Contains("site%3Aexample.test", destination.AbsoluteUri);
        Assert.True(destination.AbsoluteUri.Length > terms.Length);
    }

    [Fact]
    public async Task GeneralSettings_MissingSearchSettingDefaultsToFalseAndCanBeSaved()
    {
        var options = new DbContextOptionsBuilder<BlazorBlogsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new BlazorBlogsContext(options);
        var settingsService = new GeneralSettingsService(context);

        var missing = await settingsService.GetGeneralSettingsAsync();
        Assert.False(missing.GoogleSiteSearchEnabled);

        await settingsService.SetSettingAsync("GoogleSiteSearchEnabled", "True");
        var saved = await settingsService.GetGeneralSettingsAsync();

        Assert.True(saved.GoogleSiteSearchEnabled);
    }

    [Fact]
    public void Index_UsesConditionalAccessibleNativeGetForm()
    {
        var source = File.ReadAllText(Path.Combine(
            SchemaRegistrationTestRoot(),
            "BlazorBlogs",
            "Components",
            "Pages",
            "Index.razor"));

        Assert.Contains("@if (GoogleSiteSearchEnabled)", source, StringComparison.Ordinal);
        Assert.Contains("method=\"get\"", source, StringComparison.Ordinal);
        Assert.Contains("name=\"q\"", source, StringComparison.Ordinal);
        Assert.Contains("required", source, StringComparison.Ordinal);
        Assert.Contains("Search this site", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IJSRuntime", source, StringComparison.Ordinal);
        Assert.DoesNotContain("window.open", source, StringComparison.Ordinal);
        Assert.True(
            source.IndexOf("aria-label=\"Post category filter\"", StringComparison.Ordinal) <
            source.IndexOf("@if (GoogleSiteSearchEnabled)", StringComparison.Ordinal));
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var start = 0;
        while ((start = value.IndexOf(token, start, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            start += token.Length;
        }

        return count;
    }

    private static string SchemaRegistrationTestRoot()
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

        throw new InvalidOperationException("Could not locate the repository root.");
    }
}
