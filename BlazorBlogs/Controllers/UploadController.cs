using BlazorBlogs.Data;
using BlazorBlogs.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace BlazorBlogs
{
    public class FileObject
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int ThumbnailHeight { get; set; }
        public int ThumbnailWidth { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    // to ensure that user must be in the Administrator Role
    [Authorize(Roles = "Administrators")]
    public class UploadController : Controller
    {
        private readonly BlazorBlogsContext blogsContext;
        private readonly UserContentPaths _userContentPaths;
        private readonly DownloadStorageService _downloadStorage;

        public UploadController(
            BlazorBlogsContext context,
            UserContentPaths userContentPaths,
            DownloadStorageService downloadStorage)
        {
            this.blogsContext = context;
            _userContentPaths = userContentPaths;
            _downloadStorage = downloadStorage;
        }

        #region public async Task<IActionResult> MultipleAsync(IFormFile[] files, string CurrentDirectory)    
        [HttpPost("[action]")]
        public async Task<IActionResult> MultipleAsync(
            IFormFile[] files, string CurrentDirectory)
        {
            try
            {
                if (HttpContext.Request.Form.Files.Any())
                {
                    foreach (var file in HttpContext.Request.Form.Files)
                    {
                        // reconstruct the path to ensure everything 
                        // goes to uploads directory

                        if(CurrentDirectory == null)
                        {
                            CurrentDirectory = "";
                        }

                        var relativeDirectory = (CurrentDirectory ?? "")
                            .Replace('\\', '/')
                            .Trim('/');
                        var directory = string.IsNullOrWhiteSpace(relativeDirectory)
                            ? _userContentPaths.UploadsPath
                            : _userContentPaths.ResolvePublicPath($"uploads/{relativeDirectory}");
                        Directory.CreateDirectory(directory);
                        string path = Path.Combine(directory, Path.GetFileName(file.FileName));

                        using (var stream =
                            new FileStream(path, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }
                }
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        } 
        #endregion

        #region public async Task<IActionResult> SingleAsync(IFormFile file, string FileTitle)
        [HttpPost("[action]")]
        public async Task<IActionResult> SingleAsync(
            IFormFile file, string FileTitle, string Price)
        {
            try
            {
                if (file is null || file.Length == 0)
                    return BadRequest("Select a ZIP file to upload.");
                if (!string.Equals(Path.GetExtension(file.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Only .zip files are accepted.");
                if (!decimal.TryParse(Price, NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                    || price < 0 || price > 99999999.99m)
                    return BadRequest("Enter a valid non-negative price.");
                await using var source = file.OpenReadStream();
                var signature = new byte[2];
                if (await source.ReadAsync(signature) != 2 || signature[0] != (byte)'P' || signature[1] != (byte)'K')
                    return BadRequest("The uploaded file is not a valid ZIP archive.");
                source.Position = 0;

                var saved = await _downloadStorage.SaveAsync(file);
                try
                {
                    var uploadedFile = new Files
                    {
                        FileName = string.IsNullOrWhiteSpace(FileTitle) ? "[Unknown]" : FileTitle.Trim(),
                        FilePath = saved.StoredName,
                        OriginalFileName = saved.OriginalName,
                        Price = decimal.Round(price, 2),
                        CreateDate = DateTime.Now,
                        DownloadCount = 0
                    };
                    blogsContext.Files.Add(uploadedFile);
                    await blogsContext.SaveChangesAsync();
                    return Ok(new { fileId = uploadedFile.FileId });
                }
                catch
                {
                    _downloadStorage.Delete(saved.StoredName);
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

    }
}