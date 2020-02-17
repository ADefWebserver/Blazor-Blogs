using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorBlogs.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; }
        public bool NewsletterSubscriber { get; set; }
    }
}
