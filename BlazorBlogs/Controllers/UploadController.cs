using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorBlogs
{
    public class FileObject
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int ThumbnailHeight { get; set; }
        public int ThumbnailWidth { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    // to ensure that user must be in the Administrator Role
    [Authorize(Roles = "Administrators")]
    public class UploadController : Controller
    {
        private readonly IWebHostEnvironment environment;
        public UploadController(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> MultipleAsync(
            IFormFile[] files, string CurrentDirectory)
        {
            try
            {
                if (HttpContext.Request.Form.Files.Any())
                {
                    foreach (var file in HttpContext.Request.Form.Files)
                    {
                        // reconstruct the path to ensure everything 
                        // goes to uploads directory
                        string RequestedPath = 
                            CurrentDirectory.ToLower()
                            .Replace(environment.WebRootPath.ToLower(), "");

                        if (RequestedPath.Contains("\\uploads\\"))
                        {
                            RequestedPath = 
                                RequestedPath.Replace("\\uploads\\", "");
                        }
                        else
                        {
                            RequestedPath = "";
                        }

                        string path = 
                            Path.Combine(
                                environment.WebRootPath, 
                                "uploads", 
                                RequestedPath, 
                                file.FileName);

                        using (var stream = 
                            new FileStream(path, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }
                }
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}