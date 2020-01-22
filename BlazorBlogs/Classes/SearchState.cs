using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorBlogs
{
    public class SearchState
    {
        public int CurrentPage { get; set; }
        public string CurrentCategoryID { get; set; }
    }
}
