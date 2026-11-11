using BlazorBlogs.Data;
using Microsoft.AspNetCore.Mvc;

namespace BlazorBlogs;

[Route("api/[controller]")]
[ApiController]
public sealed class NewsletterController(
    NewsletterSubscriptionService subscriptions) : Controller
{
    [HttpPost("Unsubscribe/{token}")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OneClickUnsubscribe(
        string token,
        CancellationToken cancellationToken)
    {
        var found = await subscriptions.SetByTokenAsync(
            token,
            false,
            "OneClick",
            cancellationToken);
        return found ? Ok() : NotFound();
    }

    [HttpGet("Unsubscribe/{token}")]
    public IActionResult Preferences(string token) =>
        NewsletterSubscriptionService.IsWellFormedToken(token)
            ? Redirect($"/newsletter/preferences/{Uri.EscapeDataString(token)}")
            : NotFound();
}
