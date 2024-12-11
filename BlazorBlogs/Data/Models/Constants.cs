using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorBlogs.Models
{
    public static class Constants
    {
        // General
        public const string EmailError = "Email-Error";
        public const string EmailSent = "Email-Sent";

        // Content Types
        public const string TXT = ".TXT";
        public const string EML = ".EML";
        public const string HTML = ".HTML";
    }
}
