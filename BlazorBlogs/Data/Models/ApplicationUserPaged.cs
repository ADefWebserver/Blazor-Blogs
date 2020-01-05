using System.Collections.Generic;

namespace BlazorBlogs.Data.Models
{
    public class ApplicationUserPaged
    {
        public List<ApplicationUser> ApplicationUsers { get; set; }
        public int ApplicationUserCount { get; set; }
    }
}
