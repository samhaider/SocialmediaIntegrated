using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using System.Security.Claims;

namespace SocialMediaAPI.Controllers;

/// <summary>
/// Controller for Reddit OAuth flow and Reddit-specific operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RedditController : ControllerBase
{
    private readonly IRedditOAuthService _oauthService;
    private readonly IRedditApiService _apiService;
    private readonly ISocialAccountService _socialAccountService;
    private readonly ILogger<RedditController> _logger;

    public RedditController(
        IRedditOAuthService oauthService,
        IRedditApiService apiService,
        ISocialAccountService socialAccountService,
        ILogger<RedditController> logger)
    {
        _oauthService = oauthService;
        _apiService = apiService;
        _socialAccountService = socialAccountService;
        _logger = logger;
    }

    /// <summary>
    /// Initiate Reddit OAuth flow
    /// </summary>
    /// <param name="request">OAuth initialization request with redirect URI and state</param>
    /// <returns>Authorization URL to redirect user to</returns>
    [HttpPost("oauth/init")]
    [Authorize]
    public ActionResult<RedditOAuthInitResponse> InitiateOAuth([FromBody] RedditOAuthInitRequest request)
    {
        try
        {
            var authUrl = _oauthService.GetAuthorizationUrl(request.RedirectUri, request.State);

            return Ok(new RedditOAuthInitResponse
            {
                AuthorizationUrl = authUrl,
                State = request.State
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating Reddit OAuth");
            return BadRequest(new { message = "Failed to initiate OAuth", error = ex.Message });
        }
    }

    /// <summary>
    /// Handle OAuth callback and exchange code for tokens
    /// </summary>
    /// <param name="request">Callback request with authorization code</param>
    /// <returns>Token response with access and refresh tokens</returns>
    [HttpPost("oauth/callback")]
    [Authorize]
    public async Task<ActionResult<RedditTokenResponse>> HandleOAuthCallback([FromBody] RedditOAuthCallbackRequest request)
    {
        try
        {
            // Exchange code for tokens
            var tokenResponse = await _oauthService.ExchangeCodeForTokenAsync(request.Code, request.RedirectUri);

            if (string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return BadRequest(new { message = "Failed to obtain access token" });
            }

            // Get user info to populate account details
            var userInfo = await _oauthService.GetUserInfoAsync(tokenResponse.AccessToken);

            // Save the account connection
            var userId = GetUserId();
            var connectRequest = new ConnectSocialAccountRequest
            {
                Platform = "Reddit",
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            };

            await _socialAccountService.ConnectAccountAsync(userId, connectRequest);

            _logger.LogInformation("Reddit account connected for user {UserId}: {RedditUsername}",
                userId, userInfo.Name);

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Reddit OAuth callback");
            return BadRequest(new { message = "Failed to handle OAuth callback", error = ex.Message });
        }
    }

    /// <summary>
    /// Refresh an expired Reddit access token
    /// </summary>
    /// <param name="refreshToken">The refresh token</param>
    /// <returns>New access token</returns>
    [HttpPost("oauth/refresh")]
    [Authorize]
    public async Task<ActionResult<RedditTokenResponse>> RefreshToken([FromBody] string refreshToken)
    {
        try
        {
            var tokenResponse = await _oauthService.RefreshAccessTokenAsync(refreshToken);
            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Reddit token");
            return BadRequest(new { message = "Failed to refresh token", error = ex.Message });
        }
    }

    /// <summary>
    /// Get current Reddit user information
    /// </summary>
    /// <param name="accessToken">Reddit access token</param>
    /// <returns>Reddit user information</returns>
    [HttpGet("user")]
    [Authorize]
    public async Task<ActionResult<RedditUserInfo>> GetUserInfo([FromQuery] string accessToken)
    {
        try
        {
            var userInfo = await _oauthService.GetUserInfoAsync(accessToken);
            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Reddit user info");
            return BadRequest(new { message = "Failed to get user info", error = ex.Message });
        }
    }

    /// <summary>
    /// Submit a post to Reddit
    /// </summary>
    /// <param name="accessToken">Reddit access token</param>
    /// <param name="request">Post submission request</param>
    /// <returns>Post submission response</returns>
    [HttpPost("post")]
    [Authorize]
    public async Task<ActionResult<RedditSubmitResponse>> SubmitPost(
        [FromQuery] string accessToken,
        [FromBody] RedditPostRequest request)
    {
        try
        {
            var response = await _apiService.SubmitPostAsync(accessToken, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting Reddit post");
            return BadRequest(new { message = "Failed to submit post", error = ex.Message });
        }
    }

    /// <summary>
    /// Submit a post with media to Reddit
    /// </summary>
    /// <param name="accessToken">Reddit access token</param>
    /// <param name="subreddit">Subreddit name</param>
    /// <param name="title">Post title</param>
    /// <param name="file">Media file to upload</param>
    /// <returns>Post submission response</returns>
    [HttpPost("post/media")]
    [Authorize]
    public async Task<ActionResult<RedditSubmitResponse>> SubmitPostWithMedia(
        [FromQuery] string accessToken,
        [FromForm] string subreddit,
        [FromForm] string title,
        [FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            // Save uploaded file temporarily
            var tempPath = Path.Combine(Path.GetTempPath(), file.FileName);
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                var request = new RedditPostRequest
                {
                    Subreddit = subreddit,
                    Title = title,
                    Kind = RedditPostKind.Image
                };

                var response = await _apiService.SubmitPostWithMediaAsync(accessToken, request, tempPath);
                return Ok(response);
            }
            finally
            {
                // Clean up temp file
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting Reddit post with media");
            return BadRequest(new { message = "Failed to submit post with media", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a comment on Reddit
    /// </summary>
    /// <param name="accessToken">Reddit access token</param>
    /// <param name="request">Comment request</param>
    /// <returns>Comment response</returns>
    [HttpPost("comment")]
    [Authorize]
    public async Task<ActionResult<RedditCommentResponse>> CreateComment(
        [FromQuery] string accessToken,
        [FromBody] RedditCommentRequest request)
    {
        try
        {
            var response = await _apiService.CreateCommentAsync(accessToken, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Reddit comment");
            return BadRequest(new { message = "Failed to create comment", error = ex.Message });
        }
    }

    /// <summary>
    /// Edit a Reddit post or comment
    /// </summary>
    /// <param name="accessToken">Reddit access token</param>
    /// <param name="thingId">Full name of thing to edit (e.g., t3_xxx, t1_xxx)</param>
    /// <param name="newText">New text content</param>
    /// <returns>Success status</returns>
    [HttpPut("edit")]
    [Authorize]
    public async Task<ActionResult> EditText(
        [FromQuery] string accessToken,
        [FromQuery] string thingId,
        [FromBody] string newText)
    {
        try
        {
            var success = await _apiService.EditTextAsync(accessToken, thingId, newText);
            return success ? Ok(new { message = "Content edited successfully" })
                          : BadRequest(new { message = "Failed to edit content" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing Reddit content");
            return BadRequest(new { message = "Failed to edit content", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a Reddit post or comment
    /// </summary>
    /// <param name="accessToken">Reddit access token</param>
    /// <param name="thingId">Full name of thing to delete</param>
    /// <returns>Success status</returns>
    [HttpDelete("delete")]
    [Authorize]
    public async Task<ActionResult> Delete(
        [FromQuery] string accessToken,
        [FromQuery] string thingId)
    {
        try
        {
            var success = await _apiService.DeleteAsync(accessToken, thingId);
            return success ? Ok(new { message = "Content deleted successfully" })
                          : BadRequest(new { message = "Failed to delete content" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Reddit content");
            return BadRequest(new { message = "Failed to delete content", error = ex.Message });
        }
    }

    /// <summary>
    /// Get subreddit post requirements
    /// </summary>
    /// <param name="accessToken">Reddit access token</param>
    /// <param name="subreddit">Subreddit name (without /r/)</param>
    /// <returns>Submit requirements</returns>
    [HttpGet("subreddit/{subreddit}/requirements")]
    [Authorize]
    public async Task<ActionResult> GetSubmitRequirements(
        [FromQuery] string accessToken,
        string subreddit)
    {
        try
        {
            var requirements = await _apiService.GetSubmitRequirementsAsync(accessToken, subreddit);
            return Ok(requirements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submit requirements for r/{Subreddit}", subreddit);
            return BadRequest(new { message = "Failed to get submit requirements", error = ex.Message });
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}
