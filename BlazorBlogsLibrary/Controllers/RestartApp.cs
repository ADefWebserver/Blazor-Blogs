using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RestartAppController(IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor)
        {
            _hostEnvironment = environment;
            _httpContextAccessor = httpContextAccessor;
        }

        // api/RestartApp/ShutdownSite
        [HttpGet("[action]")]
        public ContentResult ShutdownSite()
        {
            string WebConfigOrginalFileNameAndPath = _hostEnvironment.ContentRootPath + @"\Web.config";
            string WebConfigTempFileNameAndPath = _hostEnvironment.ContentRootPath + @"\Web.config.txt";

            if (System.IO.File.Exists(WebConfigOrginalFileNameAndPath))
            {
                // Temporarily rename the web.config file
                // to release the locks on any assemblies
                System.IO.File.Copy(WebConfigOrginalFileNameAndPath, WebConfigTempFileNameAndPath);
                System.IO.File.Delete(WebConfigOrginalFileNameAndPath);

                // Give the site time to release locks on the assemblies
                Task.Delay(2000).Wait(); // Wait 2 seconds with blocking

                // Rename the temp web.config file back to web.config
                // so the site will be active again
                System.IO.File.Copy(WebConfigTempFileNameAndPath, WebConfigOrginalFileNameAndPath);
                System.IO.File.Delete(WebConfigTempFileNameAndPath);
            }

            return new ContentResult
            {
                ContentType = @"text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = $@"<html><body><h2><a href={GetBaseUrl()}>click here to continue</a></h2></body></html>"
            };
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