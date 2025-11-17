using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using System.Security.Claims;

namespace SocialMediaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpPost]
    public async Task<ActionResult<PostResponse>> CreatePost([FromBody] CreatePostRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await _postService.CreatePostAsync(userId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{postId}")]
    public async Task<ActionResult<PostResponse>> GetPost(int postId)
    {
        try
        {
            var userId = GetUserId();
            var response = await _postService.GetPostAsync(postId, userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<PostResponse>>> GetUserPosts()
    {
        try
        {
            var userId = GetUserId();
            var response = await _postService.GetUserPostsAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{postId}/publish")]
    public async Task<ActionResult> PublishPost(int postId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _postService.PublishPostAsync(postId, userId);
            return success ? Ok(new { message = "Post published successfully" }) 
                           : BadRequest(new { message = "Failed to publish post" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{postId}")]
    public async Task<ActionResult> DeletePost(int postId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _postService.DeletePostAsync(postId, userId);
            return success ? Ok(new { message = "Post deleted successfully" }) 
                           : NotFound(new { message = "Post not found" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
