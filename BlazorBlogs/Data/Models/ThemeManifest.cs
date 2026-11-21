namespace BlazorBlogs.Data.Models;

public sealed record ThemeManifest(
    string Name,
    string Slug,
    string Version,
    string Stylesheet,
    IReadOnlyList<string> Assets);
