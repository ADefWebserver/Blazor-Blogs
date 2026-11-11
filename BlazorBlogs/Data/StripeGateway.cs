using Stripe;
using Stripe.Checkout;

namespace BlazorBlogs.Data;

public sealed record StripeConnectionResult(bool Connected, bool TaxActive, string AccountId, string Message);
public sealed record StripeCheckoutCreated(string Id, string Url);
public sealed record StripeSessionResult(
    string Id, string PaymentStatus, string UserId, int FileId, string BuyerEmail,
    string PaymentIntentId, decimal Amount, decimal TaxAmount, string Currency, bool LiveMode,
    bool Refunded = false);

public interface IStripeGateway
{
    Task<StripeConnectionResult> TestConnectionAsync(StripeSettings settings, CancellationToken cancellationToken = default);
    Task<StripeCheckoutCreated> CreateCheckoutSessionAsync(
        FilesCheckoutRequest request, StripeSettings settings, CancellationToken cancellationToken = default);
    Task<StripeSessionResult> GetCheckoutSessionAsync(
        string sessionId, StripeSettings settings, CancellationToken cancellationToken = default);
    Event ParseWebhookEvent(string json, string signature, string secret);
}

public sealed record FilesCheckoutRequest(
    int FileId, string Title, decimal Price, string UserId, string BuyerEmail, string BaseUrl);

public sealed class StripeGateway : IStripeGateway
{
    public async Task<StripeConnectionResult> TestConnectionAsync(
        StripeSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new StripeClient(settings.SecretKey);
            await new BalanceService(client).GetAsync(cancellationToken: cancellationToken);
            var tax = await new Stripe.Tax.SettingsService(client).GetAsync(cancellationToken: cancellationToken);
            var active = string.Equals(tax.Status, "active", StringComparison.OrdinalIgnoreCase);
            return new(true, active, "", active ? "Connection OK" : "Stripe Tax is not active");
        }
        catch (Exception ex)
        {
            return new(false, false, "", ex.GetBaseException().Message);
        }
    }

    public async Task<StripeCheckoutCreated> CreateCheckoutSessionAsync(
        FilesCheckoutRequest request, StripeSettings settings, CancellationToken cancellationToken = default)
    {
        var baseUrl = request.BaseUrl.TrimEnd('/') + "/";
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = request.UserId ?? "",
            CustomerEmail = string.IsNullOrWhiteSpace(request.UserId) ? null : request.BuyerEmail,
            SuccessUrl = baseUrl + "checkout/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = baseUrl + "filedownloads",
            Metadata = new Dictionary<string, string>
            {
                ["fileId"] = request.FileId.ToString(),
                ["userId"] = request.UserId ?? ""
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = settings.Currency,
                        UnitAmount = StripeAmount.ToMinorUnits(request.Price, settings.Currency),
                        TaxBehavior = settings.TaxBehavior,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.Title,
                            TaxCode = string.IsNullOrWhiteSpace(settings.TaxCode) ? null : settings.TaxCode
                        }
                    }
                }
            ]
        };

        if (settings.AutomaticTax)
            options.AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true };

        var session = await new SessionService(new StripeClient(settings.SecretKey))
            .CreateAsync(options, cancellationToken: cancellationToken);
        return new(session.Id, session.Url);
    }

    public async Task<StripeSessionResult> GetCheckoutSessionAsync(
        string sessionId, StripeSettings settings, CancellationToken cancellationToken = default)
    {
        var client = new StripeClient(settings.SecretKey);
        var session = await new SessionService(client)
            .GetAsync(sessionId, cancellationToken: cancellationToken);
        var refunded = false;
        if (!string.IsNullOrWhiteSpace(session.PaymentIntentId))
        {
            var paymentIntent = await new PaymentIntentService(client)
                .GetAsync(session.PaymentIntentId, cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(paymentIntent.LatestChargeId))
            {
                var charge = await new ChargeService(client)
                    .GetAsync(paymentIntent.LatestChargeId, cancellationToken: cancellationToken);
                refunded = charge.Refunded;
            }
        }

        return ToResult(session, refunded);
    }

    public Event ParseWebhookEvent(string json, string signature, string secret) =>
        EventUtility.ConstructEvent(json, signature, secret, throwOnApiVersionMismatch: false);

    public static StripeSessionResult ToResult(Session session, bool refunded = false)
    {
        var currency = session.Currency ?? "usd";
        var fileId = 0;
        string rawFileId = null;
        session.Metadata?.TryGetValue("fileId", out rawFileId);
        int.TryParse(rawFileId, out fileId);
        string userId = null;
        session.Metadata?.TryGetValue("userId", out userId);
        return new(
            session.Id,
            session.PaymentStatus,
            userId ?? "",
            fileId,
            session.CustomerDetails?.Email ?? "",
            session.PaymentIntentId,
            StripeAmount.FromMinorUnits(session.AmountTotal ?? 0, currency),
            StripeAmount.FromMinorUnits(session.TotalDetails?.AmountTax ?? 0, currency),
            currency,
            session.Livemode,
            refunded);
    }
}
