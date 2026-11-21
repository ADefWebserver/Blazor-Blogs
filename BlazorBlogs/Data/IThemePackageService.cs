using BlazorBlogs.Data.Models;

namespace BlazorBlogs.Data;

public interface IThemePackageService
{
    Task<ThemePackage> ImportAsync(
        Stream archive,
        string fileName,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ThemePackage>> ListAsync(CancellationToken cancellationToken = default);
    Task<ThemePackage> GetAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<string> GetStylesheetUrlAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<Guid?> GetActivePackageIdAsync(CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task DeactivateAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<string> GetActiveStylesheetUrlAsync(CancellationToken cancellationToken = default);
    Task ReconcileOrphansAsync(CancellationToken cancellationToken = default);
}
