namespace BlazorBlogs.Data.Models;

public sealed class ThemePackage
{
    public Guid ThemePackageId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Version { get; set; } = "";
    public string ManifestJson { get; set; } = "";
    public string StylesheetUrl { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
}
