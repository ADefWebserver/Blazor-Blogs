namespace BlazorBlogs.Data.Models;

public sealed class SocialPreview
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required Uri CanonicalUrl { get; init; }
    public required string SiteName { get; init; }
    public Uri ImageUrl { get; init; }
    public string ImageAlt { get; init; }
    public string TwitterCard => ImageUrl is null ? "summary" : "summary_large_image";
}
