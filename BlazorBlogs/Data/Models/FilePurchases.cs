namespace BlazorBlogs.Data.Models;

public partial class FilePurchases
{
    public int Id { get; set; }
    public int FileId { get; set; }
    public string UserId { get; set; }
    public string BuyerEmail { get; set; }
    public string Status { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public string Currency { get; set; }
    public string StripeSessionId { get; set; }
    public string StripePaymentIntentId { get; set; }
    public bool LiveMode { get; set; }
    public string DownloadToken { get; set; }
    public DateTime? TokenExpiresUtc { get; set; }
    public int TokenDownloadCount { get; set; }
    public DateTime? LastLinkSentUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? PaidUtc { get; set; }
    public DateTime? RefundedUtc { get; set; }
    public Files File { get; set; }
}
