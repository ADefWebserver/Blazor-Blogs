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

        #region public async Task<UserInfo> GetUserInfoAsync(string key, string username, string password)
        public async Task<UserInfo> GetUserInfoAsync(string key, string username, string password)
        {
            UserInfo objUserInfo = new UserInfo();

            if (await IsValidMetaWeblogUserAsync(username, password))
            {
                var Blogger = await _BlazorBlogsContext.AspNetUsers
                    .Where(x => x.UserName == username)
                    .FirstOrDefaultAsync();

                objUserInfo.userid = Blogger.Id;
                objUserInfo.email = Blogger.Email;                              
                objUserInfo.lastname = Blogger.DisplayName;
                objUserInfo.nickname = Blogger.DisplayName;
                objUserInfo.firstname = "";
                objUserInfo.url = GetBaseUrl();                
            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            return objUserInfo;
        } 
        #endregion

        #region public async Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password)
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
        #endregion

        #region public async Task<Post> GetPostAsync(string postid, string username, string password)
        public async Task<Post> GetPostAsync(string postid, string username, string password)
        {
            Post objPost = new Post();

            if (await IsValidMetaWeblogUserAsync(username, password))
            {
                var Blogger = await _BlazorBlogsContext.AspNetUsers
                    .Where(x => x.UserName == username)
                    .FirstOrDefaultAsync();

                var BlogPost = await _BlazorBlogsContext.Blogs
                    .Include(x => x.BlogCategory)
                    .Where(x => x.BlogUserName == username)
                    .Where(x => x.BlogId.ToString() == postid)
                    .OrderBy(x => x.BlogDate).FirstOrDefaultAsync();

                objPost.title = BlogPost.BlogTitle;

                objPost.categories = _BlazorBlogsContext.Categorys
                    .Where(x => BlogPost.BlogCategory
                    .Select(x => x.CategoryId)
                    .Contains(x.CategoryId))
                    .Select(c => c.Title.ToString()).ToArray();

                objPost.postid = BlogPost.BlogId;
                objPost.dateCreated = BlogPost.BlogDate;
                objPost.userid = Blogger.Id;
                objPost.description = BlogPost.BlogSummary;
                objPost.wp_slug = BlogPost.BlogSummary;
                objPost.link = $"{GetBaseUrl()}/ViewBlogPost/{BlogPost.BlogId}";
                objPost.permalink = $"{GetBaseUrl()}/ViewBlogPost/{BlogPost.BlogId}";
                objPost.mt_excerpt = $"{GetBaseUrl()}/ViewBlogPost/{BlogPost.BlogId}";
            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            return objPost;
        } 
        #endregion

        #region public async Task<Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
        public async Task<Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
        {
            List<Post> Posts = new List<Post>();

            if (await IsValidMetaWeblogUserAsync(username, password))
            {
                var Blogger = await _BlazorBlogsContext.AspNetUsers
                    .Where(x => x.UserName == username)
                    .FirstOrDefaultAsync();

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
            if (await IsValidMetaWeblogUserAsync(username, password))
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public async Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish)
        {
            if (await IsValidMetaWeblogUserAsync(username, password))
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public async Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish)
        {
            if (await IsValidMetaWeblogUserAsync(username, password))
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public async Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
        {
            if (await IsValidMetaWeblogUserAsync(username, password))
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public async Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
        {
            if (await IsValidMetaWeblogUserAsync(username, password))
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public async Task<int> AddCategoryAsync(string key, string username, string password, NewCategory category)
        {
            if (await IsValidMetaWeblogUserAsync(username, password))
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public Page GetPage(string blogid, string pageid, string username, string password)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public Page[] GetPages(string blogid, string username, string password, int numPages)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public Author[] GetAuthors(string blogid, string username, string password)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public string AddPage(string blogid, string username, string password, Page page, bool publish)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public bool EditPage(string blogid, string pageid, string username, string password, Page page, bool publish)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public bool DeletePage(string blogid, string username, string password, string pageid)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public Task<Page> GetPageAsync(string blogid, string pageid, string username, string password)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public Task<Author[]> GetAuthorsAsync(string blogid, string username, string password)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
        }

        public Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid)
        {
            if (IsValidMetaWeblogUserAsync(username, password).Result)
            {

            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            throw new Exception("Bad user name or password");
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
