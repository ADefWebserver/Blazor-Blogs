using System;
using System.Collections.Generic;

namespace BlazorBlogs.Data.Models
{
    public partial class LogsPaged
    {
        public List<Logs> Logs { get; set; }
        public int LogCount { get; set; }
    }
}