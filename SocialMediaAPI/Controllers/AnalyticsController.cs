using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using System.Security.Claims;

namespace SocialMediaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpGet("posts/{postId}")]
    public async Task<ActionResult<AggregatedAnalyticsResponse>> GetPostAnalytics(int postId)
    {
        try
        {
            var userId = GetUserId();
            var response = await _analyticsService.GetPostAnalyticsAsync(postId, userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("posts/{postId}/refresh")]
    public async Task<ActionResult> RefreshPostAnalytics(int postId)
    {
        try
        {
            var userId = GetUserId();
            await _analyticsService.RefreshPostAnalyticsAsync(postId, userId);
            return Ok(new { message = "Analytics refreshed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
