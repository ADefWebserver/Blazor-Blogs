using System;
using BlazorBlogs.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Z.EntityFramework.Plus;
using BlazorBlogsLibrary.Classes;

namespace BlazorBlogs.Data
{
    public class BlogsService
    {
        private readonly BlazorBlogsContext _context;
        private readonly IWebHostEnvironment _environment;

        public BlogsService(BlazorBlogsContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Blogs

        #region public async Task<List<Blogs>> GetAllBlogsAsync(string BlogUserName)
        public async Task<List<Blogs>> GetAllBlogsAsync(string BlogUserName)
        {
            List<Blogs> colBlogs = new List<Blogs>();

            colBlogs = await (from blog in _context.Blogs
                .Include(x => x.BlogCategory)
                              select new Blogs
                              {
                                  BlogId = blog.BlogId,
                                  BlogTitle = blog.BlogTitle,
                                  BlogDate = blog.BlogDate,
                                  BlogUserName = blog.BlogUserName,
                                  BlogSummary = blog.BlogSummary,
                                  BlogContent = blog.BlogSummary,
                                  BlogCategory = blog.BlogCategory
                              }).OrderBy(x => x.BlogTitle)
                              .Where(x => x.BlogUserName.ToLower() == BlogUserName)
                              .ToListAsync();

            return colBlogs;
        }
        #endregion

        #region public async Task<BlogsPaged> GetBlogsAsync(int page, int CategoryID)
        public async Task<BlogsPaged> GetBlogsAsync(int page, int CategoryID)
        {
            page = page - 1;
            BlogsPaged objBlogsPaged = new BlogsPaged();

            if (CategoryID == 0)
            {
                objBlogsPaged.BlogCount = await _context.Blogs
                    .CountAsync();

                objBlogsPaged.Blogs = await (from blog in _context.Blogs
                    .Include(x => x.BlogCategory)
                                             select new Blogs
                                             {
                                                 BlogId = blog.BlogId,
                                                 BlogTitle = blog.BlogTitle,
                                                 BlogDate = blog.BlogDate,
                                                 BlogUserName = blog.BlogUserName,
                                                 BlogSummary = blog.BlogSummary,
                                                 BlogContent = blog.BlogSummary,
                                                 BlogCategory = blog.BlogCategory
                                             }).OrderByDescending(x => x.BlogDate).Skip(page * 5).Take(5).ToListAsync();
            }
            else
            {
                objBlogsPaged.BlogCount = await _context.Blogs
                    .Include(x => x.BlogCategory)
                    .Where(x => x.BlogCategory.Any(y => y.CategoryId == CategoryID))
                    .CountAsync();

                objBlogsPaged.Blogs = await (from blog in _context.Blogs
                    .Include(x => x.BlogCategory)
                    .Where(x => x.BlogCategory.Any(y => y.CategoryId == CategoryID))
                                             select new Blogs
                                             {
                                                 BlogId = blog.BlogId,
                                                 BlogTitle = blog.BlogTitle,
                                                 BlogDate = blog.BlogDate,
                                                 BlogUserName = blog.BlogUserName,
                                                 BlogSummary = blog.BlogSummary,
                                                 BlogContent = blog.BlogSummary,
                                                 BlogCategory = blog.BlogCategory
                                             }).OrderByDescending(x => x.BlogDate).Skip(page * 5).Take(5).ToListAsync();
            }

            return objBlogsPaged;
        }
        #endregion

        #region public BlogDTO GetBlog(int BlogId)
        public BlogDTO GetBlog(int BlogId)
        {
            BlogDTO objBlog = (from blog in _context.Blogs
                                     .Where(x => x.BlogId == BlogId)
                               select new BlogDTO
                               {
                                   BlogId = blog.BlogId,
                                   BlogTitle = blog.BlogTitle,
                                   BlogDate = blog.BlogDate,
                                   BlogUserName = blog.BlogUserName,
                                   BlogSummary = blog.BlogSummary,
                                   BlogContent = blog.BlogContent,
                                   BlogDisplayName = "",
                               }).AsNoTracking().FirstOrDefault();

            // Add Blog Categories
            objBlog.BlogCategory = new List<BlogCategory>();

            var BlogCategories = _context.BlogCategory
                .Where(x => x.BlogId == objBlog.BlogId)
                .AsNoTracking().ToList();

            foreach (var item in BlogCategories)
            {
                objBlog.BlogCategory.Add(item);
            }

            // Try to get name
            var objUser = _context.AspNetUsers
                .Where(x => x.Email.ToLower() == objBlog.BlogUserName).AsNoTracking()
                .FirstOrDefault();

            if (objUser != null)
            {
                if (objUser.DisplayName != null)
                {
                    objBlog.BlogDisplayName = objUser.DisplayName;
                }
            }

            // Get GoogleTrackingID and DisqusEnabled
            var Settings = _context.Settings.AsNoTracking().ToList();

            objBlog.DisqusEnabled = Convert.ToBoolean(Settings.FirstOrDefault(x => x.SettingName == "DisqusEnabled").SettingValue);

            if (Settings.FirstOrDefault(x => x.SettingName == "GoogleTrackingID") != null)
            {
                objBlog.GoogleTrackingID = Convert.ToString(Settings.FirstOrDefault(x => x.SettingName == "GoogleTrackingID").SettingValue);
            }
            else
            {
                objBlog.GoogleTrackingID = "";
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
                .Include(x => x.BlogCategory)
                .Where(x => x.BlogUserName == strUserName)
                .OrderByDescending(x => x.BlogDate)
                .Skip(page * 5)
                .Take(5).ToListAsync();

            return objBlogsPaged;
        }
        #endregion

        #region public Task<Blogs> CreateBlogAsync(BlogDTO newBlog, IEnumerable<String> BlogCatagories)
        public Task<Blogs> CreateBlogAsync(BlogDTO newBlog, IEnumerable<String> BlogCatagories)
        {
            try
            {
                Blogs objBlogs = new Blogs();

                objBlogs.BlogId = 0;
                objBlogs.BlogContent = newBlog.BlogContent;
                objBlogs.BlogDate = newBlog.BlogDate;
                objBlogs.BlogSummary = newBlog.BlogSummary;
                objBlogs.BlogTitle = newBlog.BlogTitle;
                objBlogs.BlogUserName = newBlog.BlogUserName;
                objBlogs.BlogContent = newBlog.BlogContent;

                if (BlogCatagories == null)
                {
                    objBlogs.BlogCategory = null;
                }
                else
                {
                    objBlogs.BlogCategory =
                        GetSelectedBlogCategories(newBlog, BlogCatagories);
                }

                _context.Blogs.Add(objBlogs);
                _context.SaveChanges();

                return Task.FromResult(objBlogs);
            }
            catch
            {
                DetachAllEntities();
                throw;
            }
        }
        #endregion

        #region public Task<bool> DeleteBlogAsync(BlogDTO objBlogs)
        public Task<bool> DeleteBlogAsync(BlogDTO objBlogs)
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

        #region public Task<bool> UpdateBlogAsync(BlogDTO objBlogs, IEnumerable<String> BlogCategories)
        public Task<bool> UpdateBlogAsync(BlogDTO objBlogs, IEnumerable<String> BlogCategories)
        {
            try
            {
                var ExistingBlogs =
                    _context.Blogs
                    .Include(x => x.BlogCategory)
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

                    if (BlogCategories == null)
                    {
                        ExistingBlogs.BlogCategory = null;
                    }
                    else
                    {
                        ExistingBlogs.BlogCategory =
                            GetSelectedBlogCategories(objBlogs, BlogCategories);
                    }

                    _context.SaveChanges();
                }
                else
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch
            {
                DetachAllEntities();
                throw;
            }
        }
        #endregion

        #region public Task<bool> UpdateBlogCategoriesAsync(Blogs objBlog, IEnumerable<String> BlogCategories)
        public Task<bool> UpdateBlogCategoriesAsync(Blogs objBlog, IEnumerable<String> BlogCategories)
        {
            try
            {
                var ExistingBlogs =
                    _context.Blogs
                    .Include(x => x.BlogCategory)
                    .Where(x => x.BlogId == objBlog.BlogId)
                    .FirstOrDefault();

                if (ExistingBlogs != null)
                {
                    if (BlogCategories == null)
                    {
                        ExistingBlogs.BlogCategory = null;
                    }
                    else
                    {
                        BlogDTO objBlogs = new BlogDTO();
                        objBlogs.BlogId = objBlog.BlogId;

                        ExistingBlogs.BlogCategory =
                            GetSelectedBlogCategories(objBlogs, BlogCategories);
                    }

                    _context.SaveChanges();
                }
                else
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch
            {
                DetachAllEntities();
                throw;
            }
        }
        #endregion

        // Category

        #region public async Task<CategoryDTO> GetCategorysAsync()
        public async Task<List<CategoryDTO>> GetCategorysAsync()
        {
            return await (from category in _context.Categorys
                          select new CategoryDTO
                          {
                              CategoryId = category.CategoryId.ToString(),
                              Description = category.Description,
                              Title = category.Title
                          }).AsNoTracking().OrderBy(x => x.Title).ToListAsync();
        }
        #endregion

        #region public async Task<CategorysPaged> GetCategorysAsync(int page)
        public async Task<CategorysPaged> GetCategorysAsync(int page)
        {
            page = page - 1;
            CategorysPaged objCategorysPaged = new CategorysPaged();

            objCategorysPaged.CategoryCount = await _context.Categorys
                 // Use AsNoTracking to disable EF change tracking
                 .AsNoTracking()
                .CountAsync();

            objCategorysPaged.Categorys = await (from category in _context.Categorys
                                                 select new CategoryDTO
                                                 {
                                                     CategoryId = category.CategoryId.ToString(),
                                                     Description = category.Description,
                                                     Title = category.Title
                                                 }).AsNoTracking()
                                                 .OrderBy(x => x.Title)
                                                 .Skip(page * 5)
                                                 .Take(5)
                                                 .ToListAsync();
            return objCategorysPaged;
        }
        #endregion

        #region public Task<bool> CreateCategoryAsync(CategoryDTO objCategoryDTO)
        public Task<bool> CreateCategoryAsync(CategoryDTO objCategoryDTO)
        {
            try
            {
                Categorys objCategorys = new Categorys();
                objCategorys.Title = objCategoryDTO.Title;
                objCategorys.Description = objCategoryDTO.Description;

                _context.Categorys.Add(objCategorys);
                _context.SaveChanges();
                return Task.FromResult(true);
            }
            catch
            {
                DetachAllEntities();
                throw;
            }
        }
        #endregion

        #region public Task<bool> UpdateCategoryAsync(CategoryDTO objCategoryDTO)
        public Task<bool> UpdateCategoryAsync(CategoryDTO objCategoryDTO)
        {
            try
            {
                int intCategoryId = Convert.ToInt32(objCategoryDTO.CategoryId);

                var ExistingCategory =
                    _context.Categorys
                    .Where(x => x.CategoryId == intCategoryId)
                    .FirstOrDefault();

                if (ExistingCategory != null)
                {
                    ExistingCategory.Title =
                        objCategoryDTO.Title;

                    ExistingCategory.Description =
                        objCategoryDTO.Description;

                    _context.SaveChanges();
                }
                else
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch
            {
                DetachAllEntities();
                throw;
            }
        }
        #endregion

        #region public Task<bool> DeleteCategoryAsync(CategoryDTO objCategoryDTO)
        public Task<bool> DeleteCategoryAsync(CategoryDTO objCategoryDTO)
        {
            int intCategoryId = Convert.ToInt32(objCategoryDTO.CategoryId);

            var ExistingCategory =
                _context.Categorys
                .Where(x => x.CategoryId == intCategoryId)
                .FirstOrDefault();

            if (ExistingCategory != null)
            {
                _context.Categorys.Remove(ExistingCategory);
                _context.SaveChanges();
            }
            else
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        #endregion

        // Files

        #region public async Task<FilesDTO> GetFilesAsync()
        public async Task<List<FilesDTO>> GetFilesAsync()
        {
            return await (from Files in _context.Files
                          select new FilesDTO
                          {
                              FileId = Files.FileId.ToString(),
                              CreateDate = Files.CreateDate,
                              DownloadCount = Files.DownloadCount,
                              FileName = Files.FileName,
                              FilePath = Files.FilePath,
                          }).AsNoTracking().OrderBy(x => x.FileName).ToListAsync();
        }
        #endregion

        #region public async Task<FilesPaged> GetFilesAsync(int page)
        public async Task<FilesPaged> GetFilesAsync(int page)
        {
            page = page - 1;
            FilesPaged objFilesPaged = new FilesPaged();

            objFilesPaged.FilesCount = await _context.Files
                 // Use AsNoTracking to disable EF change tracking
                 .AsNoTracking()
                .CountAsync();

            objFilesPaged.Files = await (from Files in _context.Files
                                         select new FilesDTO
                                         {
                                             FileId = Files.FileId.ToString(),
                                             CreateDate = Files.CreateDate,
                                             DownloadCount = Files.DownloadCount,
                                             FileName = Files.FileName,
                                             FilePath = Files.FilePath.Replace("\\files", "files"),
                                         }).AsNoTracking()
                                                 .OrderBy(x => x.FileName)
                                                 .Skip(page * 10)
                                                 .Take(10)
                                                 .ToListAsync();
            return objFilesPaged;
        }
        #endregion

        #region public Task<bool> CreateFilesAsync(FilesDTO objFilesDTO)
        public Task<bool> CreateFilesAsync(FilesDTO objFilesDTO)
        {
            try
            {
                Files objFiles = new Files();
                objFiles.CreateDate = DateTime.Now;
                objFiles.DownloadCount = 0;
                objFiles.FileName = objFilesDTO.FileName;
                objFiles.FilePath = objFilesDTO.FilePath;

                _context.Files.Add(objFiles);
                _context.SaveChanges();
                return Task.FromResult(true);
            }
            catch
            {
                DetachAllEntities();
                throw;
            }
        }
        #endregion

        #region public Task<bool> UpdateFilesAsync(FilesDTO objFilesDTO)
        public Task<bool> UpdateFilesAsync(FilesDTO objFilesDTO)
        {
            try
            {
                int intFilesId = Convert.ToInt32(objFilesDTO.FileId);

                var ExistingFiles =
                    _context.Files
                    .Where(x => x.FileId == intFilesId)
                    .FirstOrDefault();

                if (ExistingFiles != null)
                {
                    ExistingFiles.DownloadCount =
                        objFilesDTO.DownloadCount;

                    ExistingFiles.FileName =
                        objFilesDTO.FileName;

                    ExistingFiles.FilePath =
                        objFilesDTO.FilePath;

                    _context.SaveChanges();
                }
                else
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch
            {
                DetachAllEntities();
                throw;
            }
        }
        #endregion

        #region public Task<bool> DeleteFilesAsync(FilesDTO objFilesDTO)
        public Task<bool> DeleteFilesAsync(FilesDTO objFilesDTO)
        {
            int intFilesId = Convert.ToInt32(objFilesDTO.FileId);

            var ExistingFiles =
                _context.Files
                .Where(x => x.FileId == intFilesId)
                .FirstOrDefault();

            if (ExistingFiles != null)
            {
                _context.Files.Remove(ExistingFiles);
                _context.SaveChanges();

                // Delete the file
                FileController objFileController = new FileController(_environment);
                objFileController.DeleteFile(objFilesDTO.FilePath);
            }
            else
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        #endregion

        // ExternalConnections

        #region public async Task<ExternalConnectionsDTO> GetExternalConnectionsAsync()
        public async Task<List<ExternalConnectionsDTO>> GetExternalConnectionsAsync()
        {
            return await (from ExternalConnections in _context.ExternalConnections
                          select new ExternalConnectionsDTO
                          {
                              ConnectionName = ExternalConnections.ServerName + " " + ExternalConnections.DatabaseName,
                              ServerName = ExternalConnections.ServerName,
                              Id = ExternalConnections.Id,
                              DatabaseName = ExternalConnections.DatabaseName,
                              DatabaseUsername = ExternalConnections.DatabaseUsername,
                              DatabasePassword = ExternalConnections.DatabasePassword,
                              IntegratedSecurity = ExternalConnections.IntegratedSecurity,
                          }).AsNoTracking().OrderBy(x => x.ConnectionName).ToListAsync();
        }
        #endregion      

        #region public Task<ExternalConnections> CreateExternalConnectionsAsync(ExternalConnectionsDTO objExternalConnectionsDTO)
        public Task<ExternalConnections> CreateExternalConnectionsAsync(ExternalConnectionsDTO objExternalConnectionsDTO)
        {
            try
            {
                ExternalConnections objExternalConnections = new ExternalConnections();
                objExternalConnections.ServerName = objExternalConnectionsDTO.ServerName;
                objExternalConnections.DatabaseName = objExternalConnectionsDTO.DatabaseName;
                objExternalConnections.DatabaseUsername = objExternalConnectionsDTO.DatabaseUsername;
                objExternalConnections.DatabasePassword = objExternalConnectionsDTO.DatabasePassword;
                objExternalConnections.IntegratedSecurity = objExternalConnectionsDTO.IntegratedSecurity;

                _context.ExternalConnections.Add(objExternalConnections);
                _context.SaveChanges();
                return Task.FromResult(objExternalConnections);
            }
            catch
            {
                DetachAllEntities();
                throw;
            }
        }
        #endregion

        #region public Task<bool> UpdateExternalConnectionsAsync(ExternalConnectionsDTO objExternalConnectionsDTO)
        public Task<bool> UpdateExternalConnectionsAsync(ExternalConnectionsDTO objExternalConnectionsDTO)
        {
            try
            {
                int intExternalConnectionsId = Convert.ToInt32(objExternalConnectionsDTO.Id);

                var ExistingExternalConnections =
                    _context.ExternalConnections
                    .Where(x => x.Id == intExternalConnectionsId)
                    .FirstOrDefault();

                if (ExistingExternalConnections != null)
                {
                    ExistingExternalConnections.ServerName =
                        objExternalConnectionsDTO.ServerName;

                    ExistingExternalConnections.DatabaseName =
                        objExternalConnectionsDTO.DatabaseName;

                    ExistingExternalConnections.DatabaseUsername =
                        objExternalConnectionsDTO.DatabaseUsername;

                    ExistingExternalConnections.DatabasePassword =
                        objExternalConnectionsDTO.DatabasePassword;

                    ExistingExternalConnections.IntegratedSecurity =
                        objExternalConnectionsDTO.IntegratedSecurity;

                    _context.SaveChanges();
                }
                else
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch
            {
                DetachAllEntities();
                throw;
            }
        }
        #endregion

        #region public Task<bool> DeleteExternalConnectionsAsync(ExternalConnectionsDTO objExternalConnectionsDTO)
        public Task<bool> DeleteExternalConnectionsAsync(ExternalConnectionsDTO objExternalConnectionsDTO)
        {
            int intExternalConnectionsId = Convert.ToInt32(objExternalConnectionsDTO.Id);

            var ExistingExternalConnections =
                _context.ExternalConnections
                .Where(x => x.Id == intExternalConnectionsId)
                .FirstOrDefault();

            if (ExistingExternalConnections != null)
            {
                _context.ExternalConnections.Remove(ExistingExternalConnections);
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
                 // Use AsNoTracking to disable EF change tracking
                 .AsNoTracking()
                .CountAsync();

            objLogsPaged.Logs = await _context.Logs
                 // Use AsNoTracking to disable EF change tracking
                 .AsNoTracking()
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

        #region public async Task<bool> DeleteLogsAsync(string UserName)
        public async Task<bool> DeleteLogsAsync(string UserName)
        {
            await _context.Logs.AsNoTracking().DeleteAsync();

            Logs objLog = new Logs();
            objLog.LogDate = DateTime.Now;
            objLog.LogAction = "Logs cleared";
            objLog.LogUserName = UserName;
            objLog.LogIpaddress = "127.0.0.1";

            _context.Logs.Add(objLog);
            _context.SaveChanges();
            return true;
        }
        #endregion

        // Users

        #region public async Task<ApplicationUserPaged> GetUsersAsync(string paramSearch, int page)
        public async Task<ApplicationUserPaged> GetUsersAsync(string paramSearch, int page)
        {
            page = page - 1;
            ApplicationUserPaged objApplicationUserPaged = new ApplicationUserPaged();
            objApplicationUserPaged.ApplicationUsers = new List<ApplicationUser>();

            objApplicationUserPaged.ApplicationUserCount = await _context.AspNetUsers
                 // Use AsNoTracking to disable EF change tracking
                 .AsNoTracking()
                 .Where(x => x.UserName.ToLower().Contains(paramSearch)
                 || x.Email.ToLower().Contains(paramSearch)
                 || x.DisplayName.ToLower().Contains(paramSearch))
                 .CountAsync();

            var users = await _context.AspNetUsers
                 // Use AsNoTracking to disable EF change tracking
                 .AsNoTracking()
                 .Where(x => x.UserName.ToLower().Contains(paramSearch)
                 || x.Email.ToLower().Contains(paramSearch)
                 || x.DisplayName.ToLower().Contains(paramSearch))
                 .OrderByDescending(x => x.Id)
                 .Skip(page * 5)
                 .Take(5).ToListAsync();

            foreach (var item in users)
            {
                ApplicationUser objApplicationUser = new ApplicationUser();

                objApplicationUser.Id = item.Id;
                objApplicationUser.UserName = item.UserName;
                objApplicationUser.Email = item.Email;
                objApplicationUser.DisplayName = item.DisplayName;
                objApplicationUser.EmailConfirmed = item.EmailConfirmed;
                objApplicationUser.NewsletterSubscriber = (item.NewsletterSubscriber.HasValue) ? item.NewsletterSubscriber.Value : false;
                objApplicationUser.PhoneNumber = item.PhoneNumber;
                objApplicationUser.PasswordHash = "*****";

                objApplicationUserPaged.ApplicationUsers.Add(objApplicationUser);
            }

            return objApplicationUserPaged;
        }
        #endregion

        #region public async Task<bool> AdminExistsAsync(string strDefaultConnection)
        public async Task<bool> AdminExistsAsync(string strDefaultConnection)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BlazorBlogsContext>();
            optionsBuilder.UseSqlServer(strDefaultConnection);

            using (var context = new BlazorBlogsContext(optionsBuilder.Options))
            {
                // Get Admin Role
                var AdminRoleList = await context.AspNetRoles
                        // Use AsNoTracking to disable EF change tracking
                        .AsNoTracking()
                         .Where(x => x.Name.ToLower() == "Administrators")
                         .ToListAsync();

                if (AdminRoleList.Count == 0)
                {
                    return false;
                }

                var AdminRole = AdminRoleList.FirstOrDefault();

                // Get number of users in that role
                var UsersInAdminRole = await context.AspNetUserRoles
                    // Use AsNoTracking to disable EF change tracking
                    .AsNoTracking()
                    .Where(x => x.RoleId == AdminRole.Id)
                    .CountAsync();

                return (UsersInAdminRole > 0);
            }
        }
        #endregion

        #region public async Task<bool> IsDatabaseSetUpAsync(string strDefaultConnection)
        public async Task<bool> IsDatabaseSetUpAsync(string strDefaultConnection)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BlazorBlogsContext>();
            optionsBuilder.UseSqlServer(strDefaultConnection);

            try
            {
                using (var context = new BlazorBlogsContext(optionsBuilder.Options))
                {
                    // If we can get Version Number database is set up
                    var VersionNumber = await context.Settings
                            // Use AsNoTracking to disable EF change tracking
                            .AsNoTracking()
                             .Where(x => x.SettingName.ToLower() == "versionnumber")
                             .FirstOrDefaultAsync();

                    if (VersionNumber == null)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion

        // Utility

        #region private List<BlogCategory> GetSelectedBlogCategories(BlogDTO objBlogs, IEnumerable<string> blogCatagories)
        private List<BlogCategory> GetSelectedBlogCategories(BlogDTO objBlogs, IEnumerable<string> blogCatagories)
        {
            List<BlogCategory> colBlogCategory = new List<BlogCategory>();

            foreach (var item in blogCatagories)
            {
                int intBlogCatagoryId = Convert.ToInt32(item);

                // Get the Category
                var Category = _context.Categorys
                    .Where(x => x.CategoryId == intBlogCatagoryId)
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

        #region public async Task ExecuteSqlRaw(string sql)
        public async Task ExecuteSqlRaw(string sql)
        {
            await _context.Database.ExecuteSqlRawAsync(sql);
        }
        #endregion

        #region public void DetachAllEntities()
        public void DetachAllEntities()
        {
            var changedEntriesCopy = _context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();
            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }
        #endregion

    }
}
