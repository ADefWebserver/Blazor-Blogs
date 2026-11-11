using BlazorBlogs.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BlazorBlogs.Data;

public sealed class NewsletterSubscriptionService(
    BlazorBlogsContext context)
{
    public async Task<AspNetUsers> FindByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (!IsWellFormedToken(token))
            return null;

        return await context.AspNetUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.NewsletterToken == token.ToLowerInvariant(), cancellationToken);
    }

    public async Task<string> GetExistingTokenAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await context.AspNetUsers
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.NewsletterToken)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<string> EnsureTokenAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await context.AspNetUsers.SingleOrDefaultAsync(
            x => x.Id == userId,
            cancellationToken);
        if (user is null)
            return null;

        if (string.IsNullOrWhiteSpace(user.NewsletterToken))
        {
            user.NewsletterToken = CreateToken();
            await context.SaveChangesAsync(cancellationToken);
        }

        return user.NewsletterToken;
    }

    public async Task<bool> SetByTokenAsync(
        string token,
        bool subscribed,
        string source,
        CancellationToken cancellationToken = default)
    {
        if (!IsWellFormedToken(token))
            return false;

        var user = await context.AspNetUsers.SingleOrDefaultAsync(
            x => x.NewsletterToken == token.ToLowerInvariant(),
            cancellationToken);
        if (user is null)
            return false;

        SetSubscription(user, subscribed, source);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetByUserIdAsync(
        string userId,
        bool subscribed,
        string source,
        CancellationToken cancellationToken = default)
    {
        var user = await context.AspNetUsers.SingleOrDefaultAsync(
            x => x.Id == userId,
            cancellationToken);
        if (user is null)
            return false;

        SetSubscription(user, subscribed, source);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<NewsletterRecipient>> GetRecipientsAsync(
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        var sentEmails = await context.NewslettersLogs
            .AsNoTracking()
            .Where(x => x.NewsletterCampainId == campaignId
                && (x.LogType == "Email Sent" || x.LogType == "Email Sending"))
            .Select(x => x.UserName)
            .ToListAsync(cancellationToken);
        var sentSet = sentEmails
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var users = await context.AspNetUsers
            .Where(x => x.NewsletterSubscriber == true
                && x.EmailConfirmed
                && x.Email != null
                && x.Email != "")
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.NewsletterToken))
            {
                user.NewsletterToken = CreateToken();
                changed = true;
            }
        }

        if (changed)
            await context.SaveChangesAsync(cancellationToken);

        return users
            .Where(x => !string.IsNullOrWhiteSpace(x.Email) && !sentSet.Contains(x.Email.Trim()))
            .Select(x => new NewsletterRecipient(x.Id, x.Email.Trim(), x.NewsletterToken))
            .ToList();
    }

    public Task<bool> HasSentAsync(
        int campaignId,
        string email,
        CancellationToken cancellationToken = default) =>
        context.NewslettersLogs.AsNoTracking().AnyAsync(
            x => x.NewsletterCampainId == campaignId
                && (x.LogType == "Email Sent" || x.LogType == "Email Sending")
                && x.UserName == email.ToLowerInvariant(),
            cancellationToken);

    public async Task<int> GetSentCountAsync(
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        var addresses = await context.NewslettersLogs
            .AsNoTracking()
            .Where(x => x.NewsletterCampainId == campaignId && x.LogType == "Email Sent")
            .Select(x => x.UserName)
            .ToListAsync(cancellationToken);
        return addresses.Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }

    public async Task LogSourceAsync(
        string userId,
        string source,
        bool subscribed,
        CancellationToken cancellationToken = default)
    {
        var user = await context.AspNetUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return;

        AddLog(user, subscribed, source);
        await context.SaveChangesAsync(cancellationToken);
    }

    public static bool IsWellFormedToken(string token) =>
        token is { Length: 64 } && token.All(Uri.IsHexDigit);

    private static string CreateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private void SetSubscription(AspNetUsers user, bool subscribed, string source)
    {
        if (user.NewsletterSubscriber == subscribed)
            return;

        user.NewsletterSubscriber = subscribed;
        AddLog(user, subscribed, source);
    }

    private void AddLog(AspNetUsers user, bool subscribed, string source)
    {
        context.Logs.Add(new Logs
        {
            LogDate = DateTime.Now,
            LogAction = $"Newsletter {(subscribed ? "subscribed" : "unsubscribed")} via {source}",
            LogUserName = user.Email ?? user.UserName ?? user.Id,
            LogIpaddress = "127.0.0.1"
        });
    }
}
