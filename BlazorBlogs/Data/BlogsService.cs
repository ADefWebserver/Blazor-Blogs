using BlazorBlogs.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BlazorBlogs.Data
{
    public class BlogsService
    {
        private readonly BlazorBlogsContext _context;
        public BlogsService(BlazorBlogsContext context)
        {
            _context = context;
        }

        public async Task<BlogsPaged> GetBlogsAsync(int page)
        {
            page = page - 1;
            BlogsPaged objBlogsPaged = new BlogsPaged();

            objBlogsPaged.BlogCount = await _context.Blogs
                .CountAsync();

            objBlogsPaged.Blogs = await _context.Blogs
                .OrderBy(x => x.BlogDate)
                .Skip(page * 5)
                .Take(5).ToListAsync();

            return objBlogsPaged;
        }

        public Task<Blogs>
            CreateBlogAsync(Blogs objBlogs)
        {
            _context.Blogs.Add(objBlogs);
            _context.SaveChanges();

            return Task.FromResult(objBlogs);
        }

        public Task<bool>
            DeleteBlogAsync(Blogs objBlogs)
        {
            var ExistingBlogs =
                _context.Blogs
                .Where(x => x.BlogId == objBlogs.BlogId)
                .FirstOrDefault();

            if (ExistingBlogs != null)
            {
                _context.Blogs.Remove(ExistingBlogs);
                _context.SaveChanges();
            }
            else
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public Task<bool>
            UpdateBlogAsync(Blogs objBlogs)
        {
            var ExistingBlogs =
                _context.Blogs
                .Where(x => x.BlogId == objBlogs.BlogId)
                .FirstOrDefault();

            if (ExistingBlogs != null)
            {
                ExistingBlogs.BlogDate =
                    objBlogs.BlogDate;

                ExistingBlogs.BlogTitle =
                    objBlogs.BlogTitle;

                ExistingBlogs.BlogSummary =
                    objBlogs.BlogSummary;

                ExistingBlogs.BlogContent =
                    objBlogs.BlogContent;

                _context.SaveChanges();
            }
            else
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
