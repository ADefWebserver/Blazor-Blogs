namespace BlazorBlogs.Data.Models;

public sealed class StripeRefundEvents
{
    public string StripePaymentIntentId { get; set; }
    public DateTime CreatedUtc { get; set; }
}
