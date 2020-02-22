using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Authorization;
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

        public RestartAppController(IHostApplicationLifetime appLifetime)
        {
            ApplicationLifetime = appLifetime;
        }

        // api/RestartApp/ShutdownSite
        [HttpGet("[action]")]
        public void ShutdownSite()
        {
            // Only allow an Administrator
            // to call this method
            ApplicationLifetime.StopApplication();
        }
    }
}