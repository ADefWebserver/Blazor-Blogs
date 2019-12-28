using BlazorBlogs.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System;

namespace BlazorBlogs.Data
{
    public class BlogsService
    {
        private readonly BlazorBlogsContext _context;

        public BlogsService(BlazorBlogsContext context)
        {
            _context = context;
        }

        // Blogs

        #region public async Task<BlogsPaged> GetBlogsAsync(int page)
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
        #endregion

        #region public async Task<Blogs> GetBlogAsync(int BlogId)
        public async Task<Blogs> GetBlogAsync(int BlogId)
        {
            var objBlog = await _context.Blogs
                .Where(x => x.BlogId == BlogId).AsNoTracking()
                .FirstOrDefaultAsync();

            // Try to get name
            var objUser = await _context.AspNetUsers
                .Where(x => x.Email.ToLower() == objBlog.BlogUserName).AsNoTracking()
                .FirstOrDefaultAsync();

            if(objUser != null)
            {
                if(objUser.DisplayName != null)
                {
                    objBlog.BlogUserName = objUser.DisplayName;
                }
            }

            return objBlog;
        }
        #endregion

        #region public async Task<BlogsPaged> GetBlogsAdminAsync(string strUserName, int page)
        public async Task<BlogsPaged> GetBlogsAdminAsync(string strUserName, int page)
        {
            page = page - 1;
            BlogsPaged objBlogsPaged = new BlogsPaged();

            objBlogsPaged.BlogCount = await _context.Blogs
                .Where(x => x.BlogUserName == strUserName)
                .AsNoTracking()
                .CountAsync();

            objBlogsPaged.Blogs = await _context.Blogs
                .Where(x => x.BlogUserName == strUserName)
                .OrderBy(x => x.BlogDate)
                .Skip(page * 5)
                .Take(5).ToListAsync();

            return objBlogsPaged;
        }
        #endregion

        #region public Task<Blogs> CreateBlogAsync(Blogs objBlogs)
        public Task<Blogs> CreateBlogAsync(Blogs objBlogs)
        {
            _context.Blogs.Add(objBlogs);
            _context.SaveChanges();

            return Task.FromResult(objBlogs);
        }
        #endregion

        #region public Task<bool> DeleteBlogAsync(Blogs objBlogs)
        public Task<bool> DeleteBlogAsync(Blogs objBlogs)
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
        #endregion

        #region public Task<bool> UpdateBlogAsync(Blogs objBlogs)
        public Task<bool> UpdateBlogAsync(Blogs objBlogs)
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
        #endregion

        // Logs

        #region public async Task<LogsPaged> GetLogsAsync(int page)
        public async Task<LogsPaged> GetLogsAsync(int page)
        {
            page = page - 1;
            LogsPaged objLogsPaged = new LogsPaged();

            objLogsPaged.LogCount = await _context.Logs
                .CountAsync();

            objLogsPaged.Logs = await _context.Logs
                .OrderByDescending(x => x.LogId)
                .Skip(page * 10)
                .Take(10).ToListAsync();

            return objLogsPaged;
        }
        #endregion

        #region public Task<bool> CreateLogAsync(Logs objLogs)
        public Task<bool> CreateLogAsync(Logs objLogs)
        {
            _context.Logs.Add(objLogs);
            _context.SaveChanges();
            return Task.FromResult(true);
        }
        #endregion

    }
}
