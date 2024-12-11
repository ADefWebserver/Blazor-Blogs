using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorBlogsLibrary.Classes
{
    public class ExternalConnectionsDTO
    {
        public int Id { get; set; }
        public string ConnectionName { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string IntegratedSecurity { get; set; }
        public string DatabaseUsername { get; set; }
        public string DatabasePassword { get; set; }
    }
}
