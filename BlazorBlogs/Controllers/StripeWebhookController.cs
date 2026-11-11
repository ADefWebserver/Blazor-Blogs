using BlazorBlogs.Data;
using BlazorBlogs.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace BlazorBlogs;

[Route("api/[controller]")]
[ApiController]
[IgnoreAntiforgeryToken]
public class StripeWebhookController(
    StripeSettingsService settingsService,
    IStripeGateway gateway,
    PaidDownloadService paidDownloads,
    BlazorBlogsContext context) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync();
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.WebhookSecret))
        {
            await LogAsync("Stripe webhook rejected: Stripe is not configured.", cancellationToken);
            return BadRequest();
        }

        var body = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
        Event stripeEvent;
        try
        {
            stripeEvent = gateway.ParseWebhookEvent(
                body, Request.Headers["Stripe-Signature"].ToString(), settings.WebhookSecret);
        }
        catch (Exception ex)
        {
            await LogAsync($"Stripe webhook signature rejected: {ex.Message}", cancellationToken);
            return BadRequest();
        }

        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                case "checkout.session.async_payment_succeeded":
                {
                    var stripeSession = (Stripe.Checkout.Session)stripeEvent.Data.Object;
                    await paidDownloads.ConfirmCheckoutAsync(
                        stripeSession.Id,
                        $"{Request.Scheme}://{Request.Host}{Request.PathBase}/",
                        cancellationToken);
                    break;
                }
                case "checkout.session.async_payment_failed":
                    await paidDownloads.MarkStatusAsync(
                        ((Stripe.Checkout.Session)stripeEvent.Data.Object).Id, "Failed", cancellationToken);
                    break;
                case "checkout.session.expired":
                    await paidDownloads.MarkStatusAsync(
                        ((Stripe.Checkout.Session)stripeEvent.Data.Object).Id, "Expired", cancellationToken);
                    break;
                case "charge.refunded":
                {
                    var charge = (Charge)stripeEvent.Data.Object;
                    if (charge.Refunded && !string.IsNullOrWhiteSpace(charge.PaymentIntentId))
                        await paidDownloads.RefundAsync(charge.PaymentIntentId, cancellationToken);
                    break;
                }
            }
            return Ok();
        }
        catch (Exception ex)
        {
            await LogAsync($"Stripe webhook processing failed: {ex.GetBaseException().Message}", cancellationToken);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task LogAsync(string message, CancellationToken cancellationToken)
    {
        context.Logs.Add(new Logs
        {
            LogDate = DateTime.Now,
            LogAction = message,
            LogUserName = "Stripe",
            LogIpaddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}
