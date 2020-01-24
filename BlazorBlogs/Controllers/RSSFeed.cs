using BlazorBlogs.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;

namespace BlazorBlogs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RSSFeed : Controller
    {
        private readonly BlazorBlogsContext _BlazorBlogsContext;
        private readonly GeneralSettingsService _GeneralSettingsService;
        private IHttpContextAccessor _httpContextAccessor;

        public RSSFeed(BlazorBlogsContext blazorBlogsContext, 
            GeneralSettingsService generalSettingsService,
            IHttpContextAccessor httpContextAccessor)
        {
            _BlazorBlogsContext = blazorBlogsContext;
            _GeneralSettingsService = generalSettingsService;
            _httpContextAccessor = httpContextAccessor;
        }

        // From: https://mitchelsellers.com/blog/article/creating-an-rss-feed-in-asp-net-core-3-0
        [ResponseCache(Duration = 1200)]
        [HttpGet("[action]")]
        public async Task<IActionResult> RssAsync()
        {

           var objGeneralSettings = await _GeneralSettingsService.GetGeneralSettingsAsync();
           var feed = new SyndicationFeed("Title", "Description", new Uri(GetBaseUrl()), "RSSUrl", DateTime.Now);

            feed.Copyright = new TextSyndicationContent($"{DateTime.Now.Year} {objGeneralSettings.ApplicationName}");
            var items = new List<SyndicationItem>();

            var postings = _BlazorBlogsContext.Blogs.OrderByDescending(x => x.BlogDate);

            foreach (var item in postings)
            {
                string BlogURL = $"{GetBaseUrl()}/ViewBlogPost/{item.BlogId}";
                var postUrl = Url.Action("Article", "Blog", new { id = BlogURL }, HttpContext.Request.Scheme);
                var title = item.BlogTitle;
                var description = SyndicationContent.CreateHtmlContent(item.BlogSummary);
                items.Add(new SyndicationItem(title, description, new Uri(BlogURL), item.BlogId.ToString(), item.BlogDate));
            }

            feed.Items = items;

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = true,
                Indent = true
            };

            using (var stream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(stream, settings))
                {
                    var rssFormatter = new Rss20FeedFormatter(feed, false);
                    rssFormatter.WriteTo(xmlWriter);
                    xmlWriter.Flush();
                }

                return File(stream.ToArray(), "application/rss+xml; charset=utf-8");
            }
        }

        // Utility

        public string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext.Request;

            var host = request.Host.ToUriComponent();

            var pathBase = request.PathBase.ToUriComponent();

            return $"{request.Scheme}://{host}{pathBase}";
        }
    }
}
