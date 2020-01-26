using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorBlogs.Data.Models
{
    public class GeneralSettings
    {
        public string SMTPServer { get; set; }
        public string SMTPAuthendication { get; set; }
        public bool SMTPSecure { get; set; }
        public string SMTPUserName { get; set; }
        public string SMTPPassword { get; set; }
        public string SMTPFromEmail { get; set; }
        public bool AllowRegistration { get; set; }
        public bool VerifiedRegistration { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationLogo { get; set; }
        public string ApplicationHeader { get; set; }
    }
}
