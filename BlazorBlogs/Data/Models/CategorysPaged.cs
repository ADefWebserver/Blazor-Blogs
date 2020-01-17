using System;
using System.Collections.Generic;

namespace BlazorBlogs.Data.Models
{
    public partial class CategorysPaged
    {
        public List<CategoryDTO> Categorys { get; set; }
        public int CategoryCount { get; set; }
    }
}