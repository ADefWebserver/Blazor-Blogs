namespace BlazorBlogs.Data;

public sealed record CampaignSendResult(bool Succeeded, string Error = "");

public sealed record CampaignRecipientFailure(string Email, string Error);

public sealed record CampaignSendSummary(
    int Sent,
    int Failed,
    bool Cancelled,
    IReadOnlyList<CampaignRecipientFailure> Failures);
