using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlazorBlogs.Data;
using BlazorBlogs.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WilderMinds.MetaWeblog;

namespace BlazorBlogs
{
    public class MetaWeblogService : IMetaWeblogProvider
    {
        string ADMINISTRATION_ROLE = "Administrators";
        private readonly IWebHostEnvironment _environment;
        private IHttpContextAccessor _httpContextAccessor;
        private readonly BlazorBlogsContext _BlazorBlogsContext;
        private readonly GeneralSettingsService _GeneralSettingsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MetaWeblogService(
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor,
            BlazorBlogsContext blazorBlogsContext,
            GeneralSettingsService generalSettingsService,
            UserManager<ApplicationUser> userManager)
        {
            _environment = environment;
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
                objPost.description = BlogPost.BlogContent;
                objPost.wp_slug = BlogPost.BlogSummary;
                objPost.link = $"{GetBaseUrl()}/ViewBlogPost/{BlogPost.BlogId}";
                objPost.permalink = $"{GetBaseUrl()}/ViewBlogPost/{BlogPost.BlogId}";
                objPost.mt_excerpt = BlogPost.BlogSummary;
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
                    objPost.description = item.BlogContent;
                    objPost.wp_slug = item.BlogSummary;
                    objPost.link = $"{GetBaseUrl()}/ViewBlogPost/{item.BlogId}";
                    objPost.permalink = $"{GetBaseUrl()}/ViewBlogPost/{item.BlogId}";
                    objPost.mt_excerpt = item.BlogSummary;

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
            string BlogPostID = "";

            if (await IsValidMetaWeblogUserAsync(username, password))
            {
                try
                {
                    Blogs objBlogs = new Blogs();

                    objBlogs.BlogId = 0;
                    objBlogs.BlogUserName = username;

                    if (post.dateCreated > Convert.ToDateTime("1/1/1900"))
                    {
                        objBlogs.BlogDate =
                            post.dateCreated;
                    }
                    else
                    {
                        objBlogs.BlogDate = DateTime.Now;
                    }

                    objBlogs.BlogTitle =
                        post.title;

                    objBlogs.BlogContent =
                        post.description;

                    if (post.description != null)
                    {
                        string strSummary = ConvertToText(post.description);
                        int intSummaryLength = strSummary.Length;
                        if (intSummaryLength > 500)
                        {
                            intSummaryLength = 500;
                        }

                        objBlogs.BlogSummary = strSummary.Substring(0, intSummaryLength);
                    }

                    _BlazorBlogsContext.Add(objBlogs);
                    _BlazorBlogsContext.SaveChanges();
                    BlogPostID = objBlogs.BlogId.ToString();

                    if (post.categories != null)
                    {
                        objBlogs.BlogCategory =
                            GetBlogCategories(objBlogs, post.categories);
                    }

                    _BlazorBlogsContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.GetBaseException().Message);
                }
            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            return BlogPostID;
        }

        public async Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish)
        {
            if (await IsValidMetaWeblogUserAsync(username, password))
            {
                var ExistingBlogs =
                    _BlazorBlogsContext.Blogs
                    .Where(x => x.BlogId == Convert.ToInt32(postid))
                    .FirstOrDefault();

                if (ExistingBlogs != null)
                {
                    _BlazorBlogsContext.Blogs.Remove(ExistingBlogs);
                    _BlazorBlogsContext.SaveChanges();
                }
                else
                {
                    throw new Exception("Blog not found");
                }
            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            return true;
        }

        #region public async Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish)
        public async Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish)
        {
            if (await IsValidMetaWeblogUserAsync(username, password))
            {
                var ExistingBlogs = await
                                    _BlazorBlogsContext.Blogs
                                    .Include(x => x.BlogCategory)
                                    .Where(x => x.BlogId == Convert.ToInt32(postid))
                                    .FirstOrDefaultAsync();

                if (ExistingBlogs != null)
                {
                    try
                    {
                        if (post.dateCreated > Convert.ToDateTime("1/1/1900"))
                        {
                            ExistingBlogs.BlogDate =
                                post.dateCreated;
                        }

                        ExistingBlogs.BlogTitle =
                            post.title;

                        ExistingBlogs.BlogContent =
                            post.description;

                        if (post.description != null)
                        {
                            string strSummary = ConvertToText(post.description);
                            int intSummaryLength = strSummary.Length;
                            if (intSummaryLength > 500)
                            {
                                intSummaryLength = 500;
                            }

                            ExistingBlogs.BlogSummary = strSummary.Substring(0, intSummaryLength);
                        }

                        if (post.categories == null)
                        {
                            ExistingBlogs.BlogCategory = null;
                        }
                        else
                        {
                            ExistingBlogs.BlogCategory =
                                GetBlogCategories(ExistingBlogs, post.categories);
                        }

                        _BlazorBlogsContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.GetBaseException().Message);
                    }
                }
                else
                {
                    throw new Exception("Bad user name or password");
                }
            }

            return true;
        } 
        #endregion

        #region public async Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
        public async Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
        {
            List<CategoryInfo> colCategoryInfo = new List<CategoryInfo>();

            if (await IsValidMetaWeblogUserAsync(username, password))
            {
                var Categorys = await _BlazorBlogsContext.Categorys.ToListAsync();

                foreach (var item in Categorys)
                {
                    CategoryInfo objCategoryInfo = new CategoryInfo();

                    objCategoryInfo.categoryid = item.CategoryId.ToString();
                    objCategoryInfo.description = item.Description;
                    objCategoryInfo.title = item.Title;
                    objCategoryInfo.htmlUrl = GetBaseUrl();
                    objCategoryInfo.rssUrl = GetBaseUrl();

                    colCategoryInfo.Add(objCategoryInfo);
                }
            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            return colCategoryInfo.ToArray();
        }
        #endregion

        #region public async Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
        public async Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
        {
            MediaObjectInfo mediaInfo = new MediaObjectInfo();

            if (await IsValidMetaWeblogUserAsync(username, password))
            {
                string fileName = Path.GetFileName(mediaObject.name);

                string PathOnly = Path.Combine(
                    _environment.WebRootPath,
                    "blogs",
                    $"{blogid}",
                    Path.GetDirectoryName(mediaObject.name));

                if (!Directory.Exists(PathOnly))
                {
                    Directory.CreateDirectory(PathOnly);
                }

                string FilePath = Path.Combine(PathOnly, fileName);

                var fileBytes = Convert.FromBase64String(mediaObject.bits);

                if (fileBytes != null)
                {
                    using (MemoryStream ms = new MemoryStream(fileBytes))
                    {
                        Bitmap bitmap = new Bitmap(ms);

                        bitmap.Save(FilePath);
                    }
                }

                mediaInfo.url = $@"{GetBaseUrl()}/blogs/{blogid}/{Path.GetDirectoryName(mediaObject.name).Replace("\\", @"/")}/{fileName}";
            }
            else
            {
                throw new Exception("Bad user name or password");
            }

            return mediaInfo;
        }
        #endregion

        #region ** Not Implemented **
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
        #endregion

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

        #region private List<BlogCategory> GetBlogCategories(BlogDTO objBlogs, IEnumerable<string> blogCatagories)
        private List<BlogCategory> GetBlogCategories(Blogs objBlogs, IEnumerable<string> blogCatagories)
        {
            List<BlogCategory> colBlogCategory = new List<BlogCategory>();

            foreach (var item in blogCatagories)
            {
                // Get the Category
                var Category = _BlazorBlogsContext.Categorys
                    .Where(x => x.Title == item)
                    .AsNoTracking()
                    .FirstOrDefault();

                // Create a new BlogCategory
                BlogCategory NewBlogCategory = new BlogCategory();
                NewBlogCategory.BlogId = objBlogs.BlogId;
                NewBlogCategory.CategoryId = Category.CategoryId;

                // Add it to the list
                colBlogCategory.Add(NewBlogCategory);
            }

            return colBlogCategory;
        }
        #endregion

        #region ConvertToText
        public static string ConvertToText(string sHTML)
        {
            string sContent = sHTML;
            sContent = sContent.Replace("<br />", Environment.NewLine);
            sContent = sContent.Replace("<br>", Environment.NewLine);
            sContent = FormatText(sContent, true);
            return StripTags(sContent, true);
        }
        #endregion

        #region FormatText
        public static string FormatText(string HTML, bool RetainSpace)
        {
            //Match all variants of <br> tag (<br>, <BR>, <br/>, including embedded space
            string brMatch = "\\s*<\\s*[bB][rR]\\s*/\\s*>\\s*";
            //Replace Tags by replacement String and return mofified string
            return System.Text.RegularExpressions.Regex.Replace(HTML, brMatch, Environment.NewLine);
        }
        #endregion

        #region StripTags
        public static string StripTags(string HTML, bool RetainSpace)
        {
            //Set up Replacement String
            string RepString;
            if (RetainSpace)
            {
                RepString = " ";
            }
            else
            {
                RepString = "";
            }

            //Replace Tags by replacement String and return mofified string
            return System.Text.RegularExpressions.Regex.Replace(HTML, "<[^>]*>", RepString);
        }
        #endregion
    }
}
