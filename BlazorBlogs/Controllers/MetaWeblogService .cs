using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorBlogs.Data;
using BlazorBlogs.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WilderMinds.MetaWeblog;

namespace BlazorBlogs
{
    public class MetaWeblogService : IMetaWeblogProvider
    {
        string ADMINISTRATION_ROLE = "Administrators";
        private IHttpContextAccessor _httpContextAccessor;
        private readonly BlazorBlogsContext _BlazorBlogsContext;
        private readonly GeneralSettingsService _GeneralSettingsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MetaWeblogService(
            IHttpContextAccessor httpContextAccessor,
            BlazorBlogsContext blazorBlogsContext,
            GeneralSettingsService generalSettingsService,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _BlazorBlogsContext = blazorBlogsContext;
            _GeneralSettingsService = generalSettingsService;
            _userManager = userManager;
        }

        public async Task<UserInfo> GetUserInfoAsync(string key, string username, string password)
        {
            throw new NotImplementedException();
        }

        public async Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password)
        {
            BlogInfo[] colBlogInfo = new BlogInfo[1];
            colBlogInfo[0] = new BlogInfo();

            if (await IsValidMetaWeblogUserAsync(username, password))
            {
                var Blogger = _BlazorBlogsContext.AspNetUsers
                    .Where(x => x.UserName == username)
                    .FirstOrDefault();

                if (Blogger != null)
                {
                    colBlogInfo[0].blogid = Blogger.Id;
                    colBlogInfo[0].blogName = Blogger.DisplayName;
                    colBlogInfo[0].url = GetBaseUrl();
                }
            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            return colBlogInfo;
        }

        public async Task<Post> GetPostAsync(string postid, string username, string password)
        {
            throw new NotImplementedException();
        }

        #region public async Task<Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
        public async Task<Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
        {
            List<Post> Posts = new List<Post>();

            if (await IsValidMetaWeblogUserAsync(username, password))
            {
                var Blogger = _BlazorBlogsContext.AspNetUsers
                    .Where(x => x.UserName == username)
                    .FirstOrDefault();

                var BlogPosts = await _BlazorBlogsContext.Blogs
                    .Include(x => x.BlogCategory)
                    .Where(x => x.BlogUserName == username)
                    .Take(numberOfPosts)
                    .OrderBy(x => x.BlogDate).ToListAsync();

                foreach (var item in BlogPosts)
                {
                    Post objPost = new Post();
                    objPost.title = item.BlogTitle;

                    objPost.categories = _BlazorBlogsContext.Categorys
                        .Where(x => item.BlogCategory
                        .Select(x => x.CategoryId)
                        .Contains(x.CategoryId))
                        .Select(c => c.Title.ToString()).ToArray();

                    objPost.postid = item.BlogId;
                    objPost.dateCreated = item.BlogDate;
                    objPost.userid = Blogger.Id;
                    objPost.description = item.BlogSummary;
                    objPost.wp_slug = item.BlogSummary;
                    objPost.link = $"{GetBaseUrl()}/ViewBlogPost/{item.BlogId}";
                    objPost.permalink = $"{GetBaseUrl()}/ViewBlogPost/{item.BlogId}";
                    objPost.mt_excerpt = $"{GetBaseUrl()}/ViewBlogPost/{item.BlogId}";

                    Posts.Add(objPost);
                }
            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            return Posts.ToArray();
        } 
        #endregion

        public async Task<string> AddPostAsync(string blogid, string username, string password, Post post, bool publish)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish)
        {
            throw new NotImplementedException();
        }

        public async Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
        {
            throw new NotImplementedException();
        }

        public async Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
        {
            throw new NotImplementedException();
        }

        public async Task<int> AddCategoryAsync(string key, string username, string password, NewCategory category)
        {
            throw new NotImplementedException();
        }

        public Page GetPage(string blogid, string pageid, string username, string password)
        {
            throw new NotImplementedException();
        }

        public Page[] GetPages(string blogid, string username, string password, int numPages)
        {
            throw new NotImplementedException();
        }

        public Author[] GetAuthors(string blogid, string username, string password)
        {
            throw new NotImplementedException();
        }

        public string AddPage(string blogid, string username, string password, Page page, bool publish)
        {
            throw new NotImplementedException();
        }

        public bool EditPage(string blogid, string pageid, string username, string password, Page page, bool publish)
        {
            throw new NotImplementedException();
        }

        public bool DeletePage(string blogid, string username, string password, string pageid)
        {
            throw new NotImplementedException();
        }

        public Task<Page> GetPageAsync(string blogid, string pageid, string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages)
        {
            throw new NotImplementedException();
        }

        public Task<Author[]> GetAuthorsAsync(string blogid, string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish)
        {
            throw new NotImplementedException();
        }

        public Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid)
        {
            throw new NotImplementedException();
        }

        // Utility

        #region private async Task<bool> IsValidMetaWeblogUserAsync(string username, string password)
        private async Task<bool> IsValidMetaWeblogUserAsync(string username, string password)
        {
            // Get user
            var objApplicationUser = await _userManager.FindByEmailAsync(username);

            // MUst be an Administrator
            if (await _userManager.IsInRoleAsync(objApplicationUser, ADMINISTRATION_ROLE))
            {
                return await _userManager.CheckPasswordAsync(objApplicationUser, password);
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region public string GetBaseUrl()
        public string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext.Request;

            var host = request.Host.ToUriComponent();

            var pathBase = request.PathBase.ToUriComponent();

            return $"{request.Scheme}://{host}{pathBase}";
        }
        #endregion
    }
}
