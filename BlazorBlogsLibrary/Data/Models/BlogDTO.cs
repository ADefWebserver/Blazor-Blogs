using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorBlogs.Data.Models
{
    public class BlogDTO
    {
        public int BlogId { get; set; }
        public DateTime BlogDate { get; set; }
        public string BlogTitle { get; set; }
        public string BlogSummary { get; set; }
        public string BlogContent { get; set; }
        public string BlogUserName { get; set; }
        public string BlogDisplayName { get; set; }
        public string GoogleTrackingID { get; set; }
        public bool DisqusEnabled { get; set; }
        public List<BlogCategory> BlogCategory { get; set; }
    }
}
