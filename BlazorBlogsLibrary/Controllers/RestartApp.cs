using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace BlazorBlogs
{
    [Route("api/[controller]")]
    [ApiController]
    // to ensure that user must be in the Administrator Role
    [Authorize(Roles = "Administrators")]
    public class RestartAppController : Controller
    {
        private IHostApplicationLifetime ApplicationLifetime { get; set; }
        private IHttpContextAccessor _httpContextAccessor;

        public RestartAppController(IHostApplicationLifetime appLifetime,
            IHttpContextAccessor httpContextAccessor)
        {
            ApplicationLifetime = appLifetime;
            _httpContextAccessor = httpContextAccessor;
        }

        // api/RestartApp/ShutdownSite
        [HttpGet("[action]")]
        public void ShutdownSite(string url)
        {
            ApplicationLifetime.StopApplication();

            //return new ContentResult
            //{
            //    ContentType = @"text/html",
            //    StatusCode = (int)HttpStatusCode.OK,
            //    Content = $@"<html><body><h2><a href={GetBaseUrl()}>click here to continue</a></h2></body></html>"
            //};
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