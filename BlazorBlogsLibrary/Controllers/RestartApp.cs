using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace BlazorBlogs
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestartAppController : Controller
    {
        private IHostApplicationLifetime ApplicationLifetime { get; set; }

        public RestartAppController(IHostApplicationLifetime appLifetime)
        {
            ApplicationLifetime = appLifetime;
        }

        // /api/RestartApp/ShutdownSite
        [HttpGet("[action]")]
        public RestartAppModel ShutdownSite()
        {
            // In a real application only allow an Administrator
            // to call this method
            ApplicationLifetime.StopApplication();
            return new RestartAppModel() { Messsage = "Done", IsSuccess = true };
        }
    }
}