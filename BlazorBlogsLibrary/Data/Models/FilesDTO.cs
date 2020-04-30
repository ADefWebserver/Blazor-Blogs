using System;
using System.Collections.Generic;

namespace BlazorBlogs.Data.Models
{
    public partial class FilesPaged
    {
        public List<FilesDTO> Files { get; set; }
        public int FilesCount { get; set; }
    }

    public partial class FilesDTO
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime CreateDate { get; set; }
        public int DownloadCount { get; set; }
    }
}