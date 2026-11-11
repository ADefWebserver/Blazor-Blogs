using System.Security.Cryptography;
using BlazorBlogs.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BlazorBlogs.Data;

public sealed record CheckoutResult(string SessionId, string Url);
public sealed record DownloadFulfilmentResult(
    bool Paid, int FileId, string FileTitle, string BuyerEmail, decimal Amount,
    decimal TaxAmount, string Currency, string DownloadToken, DateTime? TokenExpiresUtc);

public sealed class PaidDownloadService(
    BlazorBlogsContext context,
    StripeSettingsService stripeSettings,
    IStripeGateway gateway,
    EmailService emailService,
    SiteUrlService siteUrlService)
{
    public async Task<CheckoutResult> StartCheckoutAsync(
        Files file, ApplicationUser user, string fallbackBaseUrl, CancellationToken cancellationToken = default)
    {
        var settings = await stripeSettings.GetAsync();
        if (!settings.Enabled || !settings.IsConfigured)
            throw new InvalidOperationException("Stripe checkout is not available.");
        if (file.Price <= 0)
            throw new InvalidOperationException("This download does not require payment.");
        if (user is not null && await OwnsFileAsync(file.FileId, user, cancellationToken))
            throw new InvalidOperationException("You already own this download.");

        var baseUrl = await siteUrlService.GetBaseUrlAsync(fallbackBaseUrl);
        var session = await gateway.CreateCheckoutSessionAsync(
            new FilesCheckoutRequest(
                file.FileId,
                file.FileName,
                file.Price,
                user?.Id,
                user?.Email,
                baseUrl),
            settings,
            cancellationToken);

        context.FilePurchases.Add(new FilePurchases
        {
            FileId = file.FileId,
            UserId = user?.Id,
            Status = "Pending",
            Amount = file.Price,
            TaxAmount = 0,
            Currency = settings.Currency,
            StripeSessionId = session.Id,
            LiveMode = session.Id.StartsWith("cs_live_", StringComparison.OrdinalIgnoreCase),
            CreatedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);
        return new CheckoutResult(session.Id, session.Url);
    }

    public async Task<DownloadFulfilmentResult> ConfirmCheckoutAsync(
        string sessionId, string fallbackBaseUrl = null, CancellationToken cancellationToken = default)
    {
        var settings = await stripeSettings.GetAsync();
        var session = await gateway.GetCheckoutSessionAsync(sessionId, settings, cancellationToken);
        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            return new(false, session.FileId, "", session.BuyerEmail, session.Amount, session.TaxAmount, session.Currency, "", null);

        return await FulfilAsync(session, fallbackBaseUrl, cancellationToken);
    }

    public async Task<DownloadFulfilmentResult> FulfilAsync(
        StripeSessionResult session, string fallbackBaseUrl = null, CancellationToken cancellationToken = default)
    {
        var exists = await context.FilePurchases.AsNoTracking()
            .AnyAsync(x => x.StripeSessionId == session.Id, cancellationToken);
        if (!exists)
        {
            context.FilePurchases.Add(new FilePurchases
            {
                FileId = session.FileId,
                UserId = string.IsNullOrWhiteSpace(session.UserId) ? null : session.UserId,
                BuyerEmail = session.BuyerEmail,
                Status = "Pending",
                Amount = session.Amount,
                TaxAmount = session.TaxAmount,
                Currency = session.Currency,
                StripeSessionId = session.Id,
                LiveMode = session.LiveMode,
                CreatedUtc = DateTime.UtcNow
            });
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                context.ChangeTracker.Clear();
            }
        }

        var now = DateTime.UtcNow;
        var settings = await stripeSettings.GetAsync();
        var refundObserved = !string.IsNullOrWhiteSpace(session.PaymentIntentId)
            && await context.StripeRefundEvents.AsNoTracking()
                .AnyAsync(
                    x => x.StripePaymentIntentId == session.PaymentIntentId,
                    cancellationToken);
        if (session.Refunded || refundObserved)
        {
            await context.Database.ExecuteSqlAsync($"""
                UPDATE [FilePurchases]
                   SET [Status] = 'Refunded',
                       [BuyerEmail] = {session.BuyerEmail},
                       [StripePaymentIntentId] = {session.PaymentIntentId},
                       [Amount] = {session.Amount},
                       [TaxAmount] = {session.TaxAmount},
                       [Currency] = {session.Currency},
                       [LiveMode] = {session.LiveMode},
                       [DownloadToken] = NULL,
                       [TokenExpiresUtc] = NULL,
                       [RefundedUtc] = {now}
                 WHERE [StripeSessionId] = {session.Id}
                """, cancellationToken);

            var refunded = await context.FilePurchases.AsNoTracking()
                .Include(x => x.File)
                .SingleAsync(x => x.StripeSessionId == session.Id, cancellationToken);
            return ToResult(refunded);
        }

        var token = string.IsNullOrWhiteSpace(session.UserId) ? NewToken() : null;
        var expires = token is null ? (DateTime?)null : now.AddDays(settings.GuestLinkDays);
        var changed = await context.Database.ExecuteSqlAsync($"""
            UPDATE [FilePurchases]
               SET [Status] = CASE
                       WHEN EXISTS (
                           SELECT 1
                           FROM [StripeRefundEvents]
                           WHERE [StripePaymentIntentId] = {session.PaymentIntentId})
                       THEN 'Refunded'
                       ELSE 'Paid'
                   END,
                   [BuyerEmail] = {session.BuyerEmail},
                   [StripePaymentIntentId] = {session.PaymentIntentId},
                   [Amount] = {session.Amount},
                   [TaxAmount] = {session.TaxAmount},
                   [Currency] = {session.Currency},
                   [LiveMode] = {session.LiveMode},
                   [PaidUtc] = CASE
                       WHEN EXISTS (
                           SELECT 1
                           FROM [StripeRefundEvents]
                           WHERE [StripePaymentIntentId] = {session.PaymentIntentId})
                       THEN NULL
                       ELSE {now}
                   END,
                   [RefundedUtc] = CASE
                       WHEN EXISTS (
                           SELECT 1
                           FROM [StripeRefundEvents]
                           WHERE [StripePaymentIntentId] = {session.PaymentIntentId})
                       THEN {now}
                       ELSE NULL
                   END,
                   [DownloadToken] = CASE
                       WHEN EXISTS (
                           SELECT 1
                           FROM [StripeRefundEvents]
                           WHERE [StripePaymentIntentId] = {session.PaymentIntentId})
                       THEN NULL
                       ELSE {token}
                   END,
                   [TokenExpiresUtc] = CASE
                       WHEN EXISTS (
                           SELECT 1
                           FROM [StripeRefundEvents]
                           WHERE [StripePaymentIntentId] = {session.PaymentIntentId})
                       THEN NULL
                       ELSE {expires}
                   END
             WHERE [StripeSessionId] = {session.Id} AND [Status] = 'Pending'
            """, cancellationToken);

        var purchase = await context.FilePurchases.AsNoTracking()
            .Include(x => x.File)
            .SingleAsync(x => x.StripeSessionId == session.Id, cancellationToken);
        var result = ToResult(purchase);
        if (changed == 1 && result.Paid)
            await SendReceiptAsync(purchase, fallbackBaseUrl, cancellationToken);
        return result;
    }

    public async Task<bool> OwnsFileAsync(
        int fileId, ApplicationUser user, CancellationToken cancellationToken = default)
    {
        if (user is null)
            return false;
        var email = user.EmailConfirmed ? user.Email : null;
        return await context.FilePurchases.AsNoTracking().AnyAsync(
            x => x.FileId == fileId && x.Status == "Paid"
                && (x.UserId == user.Id || (email != null && x.BuyerEmail != null && x.BuyerEmail.ToLower() == email.ToLower())),
            cancellationToken);
    }

    public async Task<HashSet<int>> GetOwnedFileIdsAsync(
        ApplicationUser user, CancellationToken cancellationToken = default)
    {
        if (user is null)
            return [];
        var email = user.EmailConfirmed ? user.Email : null;
        return (await context.FilePurchases.AsNoTracking()
            .Where(x => x.Status == "Paid" &&
                (x.UserId == user.Id || (email != null && x.BuyerEmail != null && x.BuyerEmail.ToLower() == email.ToLower())))
            .Select(x => x.FileId)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToHashSet();
    }

    public async Task<bool> CanDeleteFileAsync(int fileId, CancellationToken cancellationToken = default) =>
        !await context.FilePurchases.AsNoTracking()
            .AnyAsync(x => x.FileId == fileId && (x.Status == "Paid" || x.Status == "Refunded"), cancellationToken);

    public async Task<bool> RecordDownloadAsync(
        int fileId, int? purchaseId, int maxDownloads, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (purchaseId.HasValue)
            {
                var counted = await context.Database.ExecuteSqlAsync(
                    $"""
                     UPDATE [FilePurchases]
                        SET [TokenDownloadCount] = [TokenDownloadCount] + 1
                      WHERE [Id] = {purchaseId.Value}
                        AND [Status] = 'Paid'
                        AND [TokenDownloadCount] < {maxDownloads}
                     """, cancellationToken);
                if (counted != 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            await context.Database.ExecuteSqlAsync(
                $"UPDATE [Files] SET [DownloadCount] = [DownloadCount] + 1 WHERE [FileId] = {fileId}",
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task MarkStatusAsync(string sessionId, string status, CancellationToken cancellationToken = default)
    {
        var purchase = await context.FilePurchases.SingleOrDefaultAsync(x => x.StripeSessionId == sessionId, cancellationToken);
        if (purchase is not null && purchase.Status == "Pending")
        {
            purchase.Status = status;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RefundAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await context.Database.ExecuteSqlAsync($"""
            IF NOT EXISTS (
                SELECT 1 FROM [StripeRefundEvents]
                WHERE [StripePaymentIntentId] = {paymentIntentId})
            BEGIN
                INSERT INTO [StripeRefundEvents] ([StripePaymentIntentId], [CreatedUtc])
                VALUES ({paymentIntentId}, {now})
            END
            """, cancellationToken);
        await context.Database.ExecuteSqlAsync($"""
            UPDATE [FilePurchases]
               SET [Status] = 'Refunded',
                   [RefundedUtc] = {now},
                   [DownloadToken] = NULL,
                   [TokenExpiresUtc] = NULL
             WHERE [StripePaymentIntentId] = {paymentIntentId} AND [Status] <> 'Refunded'
            """, cancellationToken);
    }

    public async Task ResendAsync(string email, string fallbackBaseUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;
        var settings = await stripeSettings.GetAsync();
        var purchases = await context.FilePurchases
            .Include(x => x.File)
            .Where(x => x.Status == "Paid" && x.UserId == null && x.BuyerEmail != null && x.BuyerEmail.ToLower() == email.Trim().ToLower())
            .ToListAsync(cancellationToken);

        foreach (var purchase in purchases)
        {
            var now = DateTime.UtcNow;
            var token = NewToken();
            var expires = now.AddDays(settings.GuestLinkDays);
            var throttle = now.AddMinutes(-15);
            var changed = await context.Database.ExecuteSqlAsync($"""
                UPDATE [FilePurchases]
                   SET [DownloadToken] = {token},
                       [TokenExpiresUtc] = {expires},
                       [TokenDownloadCount] = 0,
                       [LastLinkSentUtc] = {now}
                 WHERE [Id] = {purchase.Id}
                   AND ([LastLinkSentUtc] IS NULL OR [LastLinkSentUtc] <= {throttle})
                """, cancellationToken);
            if (changed == 1)
            {
                context.Entry(purchase).State = EntityState.Detached;
                var refreshed = await context.FilePurchases.Include(x => x.File)
                    .SingleAsync(x => x.Id == purchase.Id, cancellationToken);
                await SendReceiptAsync(refreshed, fallbackBaseUrl, cancellationToken);
            }
        }
    }

    public async Task<bool> ResendPurchaseAsync(int purchaseId, string fallbackBaseUrl, CancellationToken cancellationToken = default)
    {
        var purchase = await context.FilePurchases.AsNoTracking()
            .Include(x => x.File)
            .SingleOrDefaultAsync(x => x.Id == purchaseId, cancellationToken);
        if (purchase?.Status != "Paid" || string.IsNullOrWhiteSpace(purchase.BuyerEmail))
            return false;
        if (string.IsNullOrWhiteSpace(purchase.UserId))
            await ResendAsync(purchase.BuyerEmail, fallbackBaseUrl, cancellationToken);
        else
            await SendReceiptAsync(purchase, fallbackBaseUrl, cancellationToken);
        return true;
    }

    public async Task SendReceiptAsync(
        FilePurchases purchase, string fallbackBaseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(purchase.BuyerEmail))
                return;
            fallbackBaseUrl ??= purchase.StripeSessionId.StartsWith("cs_live_", StringComparison.OrdinalIgnoreCase)
                ? "https://localhost/"
                : "http://localhost/";
            var baseUrl = await siteUrlService.GetBaseUrlAsync(fallbackBaseUrl);
            var link = string.IsNullOrWhiteSpace(purchase.DownloadToken)
                ? baseUrl + "filedownloads"
                : baseUrl + $"api/File/Download/{purchase.FileId}?t={Uri.EscapeDataString(purchase.DownloadToken)}";
            var fileTitle = purchase.File?.FileName ?? (await context.Files.FindAsync([purchase.FileId], cancellationToken))?.FileName ?? "your download";
            var body = $"<p>Thank you for purchasing <strong>{System.Net.WebUtility.HtmlEncode(fileTitle)}</strong>.</p>" +
                       $"<p>Total: {purchase.Amount:0.00} {purchase.Currency.ToUpperInvariant()} (tax: {purchase.TaxAmount:0.00}).</p>" +
                       $"<p><a href=\"{link}\">Download your file</a></p>";
            var result = await emailService.SendMailAsync(false, purchase.BuyerEmail, purchase.BuyerEmail, "", "", "", "Your download receipt", body);
            if (!string.IsNullOrEmpty(result))
            {
                await LogAsync($"Paid download receipt could not be sent: {result}", purchase.BuyerEmail, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            try
            {
                await LogAsync($"Paid download receipt could not be sent: {ex.GetBaseException().Message}", purchase.BuyerEmail, CancellationToken.None);
            }
            catch
            {
                // Fulfilment must remain successful even when receipt delivery and logging fail.
            }
        }
    }

    private async Task LogAsync(string message, string user, CancellationToken cancellationToken)
    {
        context.Logs.Add(new Logs
        {
            LogDate = DateTime.Now,
            LogAction = message,
            LogUserName = user ?? "",
            LogIpaddress = "127.0.0.1"
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static DownloadFulfilmentResult ToResult(FilePurchases purchase) => new(
        purchase.Status == "Paid",
        purchase.FileId,
        purchase.File?.FileName ?? "",
        purchase.BuyerEmail ?? "",
        purchase.Amount,
        purchase.TaxAmount,
        purchase.Currency,
        purchase.DownloadToken ?? "",
        purchase.TokenExpiresUtc);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);
}
