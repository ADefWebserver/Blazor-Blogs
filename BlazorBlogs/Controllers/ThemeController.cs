using BlazorBlogs.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlazorBlogs.Controllers;

[ApiController]
[Route("api/themes")]
[Authorize(Roles = "Administrators")]
[AutoValidateAntiforgeryToken]
public sealed class ThemeController(IThemePackageService themes) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await themes.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var package = await themes.GetAsync(id, cancellationToken);
        return package is null ? NotFound() : Ok(package);
    }

    [HttpPost("import")]
    [RequestSizeLimit(ThemePackageValidator.MaxArchiveBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = ThemePackageValidator.MaxArchiveBytes + 64 * 1024)]
    public async Task<IActionResult> Import(
        [FromForm(Name = "archive")] IFormFile archive,
        CancellationToken cancellationToken)
    {
        if (archive is null || archive.Length == 0)
            return BadRequest(new { error = "Select a theme ZIP archive." });
        if (archive.Length > ThemePackageValidator.MaxArchiveBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);

        try
        {
            await using var stream = archive.OpenReadStream();
            var package = await themes.ImportAsync(stream, archive.FileName, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = package.ThemePackageId }, package);
        }
        catch (ThemePackageTooLargeException)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }
        catch (ThemePackageValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ThemePackageConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "A package with this identity is already installed." });
        }
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await themes.ActivateAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ThemePackageValidationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate(CancellationToken cancellationToken)
    {
        await themes.DeactivateAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (await themes.GetAsync(id, cancellationToken) is null)
            return NotFound();
        try
        {
            await themes.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (ThemePackageConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
