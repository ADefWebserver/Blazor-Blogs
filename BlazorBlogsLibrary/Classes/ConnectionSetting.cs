using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorBlogsLibrary.Classes.Imports
{
    public class ConnectionSetting
    {
        public string DatabaseName { get; set; }
        public string ServerName { get; set; }
        public bool IntegratedSecurity { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class DTOConnectionSetting
    {
        public string DatabaseName { get; set; }
        public string ServerName { get; set; }
        public bool IntegratedSecurity { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class DTOStatus
    {
        public string StatusMessage { get; set; }
        public bool Success { get; set; }
    }
}
