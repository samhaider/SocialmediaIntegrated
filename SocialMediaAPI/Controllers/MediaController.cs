using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAPI.Interfaces;
using System.Security.Claims;

namespace SocialMediaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpPost("upload")]
    public async Task<ActionResult<object>> UploadMedia(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        try
        {
            var userId = GetUserId();
            using var stream = file.OpenReadStream();
            var url = await _mediaService.UploadMediaAsync(userId, stream, file.FileName, file.ContentType);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{mediaId}")]
    public async Task<ActionResult> DeleteMedia(int mediaId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _mediaService.DeleteMediaAsync(mediaId, userId);
            return success ? Ok(new { message = "Media deleted successfully" }) 
                           : NotFound(new { message = "Media not found" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
