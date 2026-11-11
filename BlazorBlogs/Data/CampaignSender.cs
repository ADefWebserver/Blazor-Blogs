using BlazorBlogs.Data.Models;
using Microsoft.AspNetCore.Hosting;
using System.Drawing;
using System.Text.RegularExpressions;

namespace BlazorBlogs.Data;

public sealed class CampaignSender(
    NewsletterSubscriptionService subscriptions,
    BlogsService blogsService,
    UserContentPaths paths,
    IWebHostEnvironment environment)
{
    public async Task<CampaignSendSummary> SendAsync(
        NewslettersCampain campaign,
        IReadOnlyList<NewsletterRecipient> recipients,
        string subject,
        string content,
        string senderEmail,
        string baseUrl,
        Func<int, int, Task> progress,
        CancellationToken cancellationToken)
    {
        var failures = new List<CampaignRecipientFailure>();
        var sent = 0;
        var cancelled = false;
        var images = await LoadImagesAsync(content, baseUrl, cancellationToken);

        for (var index = 0; index < recipients.Count; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            var recipient = recipients[index];
            if (await subscriptions.HasSentAsync(campaign.Id, recipient.Email, cancellationToken))
            {
                await progress(sent, failures.Count);
                continue;
            }

            var result = await blogsService.SendNewslettersCampainEmailAsync(
                recipient,
                campaign,
                subject,
                content,
                images,
                senderEmail,
                baseUrl,
                logCampaign: true,
                cancellationToken);

            if (result.Succeeded)
                sent++;
            else
                failures.Add(new CampaignRecipientFailure(recipient.Email, result.Error));

            await progress(sent, failures.Count);

            if (index < recipients.Count - 1)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    break;
                }
            }
        }

        return new CampaignSendSummary(sent, failures.Count, cancelled, failures);
    }

    public async Task<Dictionary<string, Image>> LoadImagesAsync(
        string content,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var images = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(
                     content ?? "",
                     @"<img\b[^>]*\bsrc\s*=\s*[""']([^""']+)[""']",
                     RegexOptions.IgnoreCase))
        {
            var source = match.Groups[1].Value;
            if (Uri.TryCreate(source, UriKind.Absolute, out _)
                || source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                continue;

            var relative = Uri.UnescapeDataString(source.Split('?', '#')[0])
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            try
            {
                var path = paths.ResolvePublicPath(relative);
                if (!File.Exists(path))
                {
                    var fallback = Path.GetFullPath(Path.Combine(environment.WebRootPath, relative));
                    var webRootPrefix = environment.WebRootPath.TrimEnd(
                        Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (!fallback.StartsWith(webRootPrefix, StringComparison.OrdinalIgnoreCase)
                        || !File.Exists(fallback))
                        continue;
                    path = fallback;
                }

                var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
                using var stream = new MemoryStream(bytes);
                using var image = Image.FromStream(stream, false, true);
                images[source] = (Image)image.Clone();
            }
            catch
            {
                // A missing or unreadable image is left as an absolute link in the message.
            }
        }

        return images;
    }
}
