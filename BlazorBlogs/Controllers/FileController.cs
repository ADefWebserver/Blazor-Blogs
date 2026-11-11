using BlazorBlogs.Data;
using BlazorBlogs.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace BlazorBlogs;

[Route("api/[controller]")]
[ApiController]
public class FileController(
    BlazorBlogsContext context,
    UserManager<ApplicationUser> userManager,
    DownloadStorageService storage,
    PaidDownloadService paidDownloads,
    StripeSettingsService stripeSettings,
    IDataProtectionProvider dataProtection) : Controller
{
    private const string GuestDownloadCookie = ".BlazorBlogs.GuestDownload";
    private readonly IDataProtector guestDownloadProtector =
        dataProtection.CreateProtector("BlazorBlogs.GuestDownload.v1");

    [HttpGet("Download/{fileId:int}")]
    public async Task<IActionResult> Download(
        int fileId, string t, CancellationToken cancellationToken)
    {
        var file = await context.Files.AsNoTracking()
            .SingleOrDefaultAsync(x => x.FileId == fileId, cancellationToken);
        if (file is null)
            return NotFound();

        var path = storage.Resolve(file.FilePath);
        if (!System.IO.File.Exists(path))
            return NotFound();

        ApplicationUser user = null;
        if (User.Identity?.IsAuthenticated == true)
            user = await userManager.GetUserAsync(User);

        var isAdmin = User.IsInRole("Administrators");
        FilePurchases guestPurchase = null;
        if (!isAdmin && file.Price > 0)
        {
            if (user is not null && await paidDownloads.OwnsFileAsync(fileId, user, cancellationToken))
            {
                // Signed-in entitlement can be by user id or confirmed email.
            }
            else if (!string.IsNullOrWhiteSpace(t))
            {
                guestPurchase = await context.FilePurchases.AsNoTracking()
                    .SingleOrDefaultAsync(x =>
                        x.FileId == fileId && x.DownloadToken == t && x.Status == "Paid", cancellationToken);
                if (guestPurchase is null)
                    return StatusCode(StatusCodes.Status403Forbidden);
            }
            else
            {
                return Redirect($"/filedownloads?buy={fileId}");
            }

            if (guestPurchase is not null)
            {
                var settings = await stripeSettings.GetAsync();
                if (guestPurchase.TokenExpiresUtc is null
                    || guestPurchase.TokenExpiresUtc <= DateTime.UtcNow
                    || guestPurchase.TokenDownloadCount >= settings.GuestMaxDownloads)
                    return Redirect("/downloads/resend");
            }
        }
        else if (!isAdmin && user is null)
        {
            var returnUrl = Uri.EscapeDataString($"/api/File/Download/{fileId}");
            return Redirect($"/Account/Login?ReturnUrl={returnUrl}");
        }

        var range = Request.GetTypedHeaders().Range?.Ranges.FirstOrDefault();
        var hasGuestContinuation = guestPurchase is not null && HasValidGuestContinuation(
            fileId, guestPurchase, t);
        if (guestPurchase is not null && range is not null && range.From != 0 && !hasGuestContinuation)
            return StatusCode(StatusCodes.Status403Forbidden);

        var shouldCount = range is null || range.From == 0 || !hasGuestContinuation;
        if (shouldCount)
        {
            var settings = guestPurchase is null ? null : await stripeSettings.GetAsync();
            if (!await paidDownloads.RecordDownloadAsync(
                fileId,
                guestPurchase?.Id,
                settings?.GuestMaxDownloads ?? 0,
                cancellationToken))
                return Redirect("/downloads/resend");

            if (guestPurchase is not null)
            {
                Response.Cookies.Append(
                    GuestDownloadCookie,
                    guestDownloadProtector.Protect($"{fileId}:{guestPurchase.Id}:{guestPurchase.DownloadToken}"),
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        IsEssential = true,
                        MaxAge = TimeSpan.FromMinutes(10)
                    });
            }
        }

        await LogDownloadAsync(user?.Id ?? "anonymous", file.FileName, cancellationToken);
        var downloadName = string.IsNullOrWhiteSpace(file.OriginalFileName)
            ? DownloadStorageService.GetFileName(file.FilePath)
            : file.OriginalFileName;
        return PhysicalFile(path, "application/zip", downloadName, enableRangeProcessing: true);
    }

    private bool HasValidGuestContinuation(int fileId, FilePurchases purchase, string token)
    {
        if (!Request.Cookies.TryGetValue(GuestDownloadCookie, out var protectedValue))
            return false;

        try
        {
            return string.Equals(
                guestDownloadProtector.Unprotect(protectedValue),
                $"{fileId}:{purchase.Id}:{token}",
                StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is CryptographicException or InvalidOperationException)
        {
            return false;
        }
    }

    [HttpGet("/files/{**path}")]
    public IActionResult LegacyFilesRedirect(string path) => Redirect("/filedownloads");

    [HttpGet("DownloadFile")]
    public IActionResult RemovedDownloadFile() => NotFound();

    private async Task LogDownloadAsync(string user, string title, CancellationToken cancellationToken)
    {
        context.Logs.Add(new Logs
        {
            LogDate = DateTime.Now,
            LogAction = $"Download File #{title}",
            LogUserName = user,
            LogIpaddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}
