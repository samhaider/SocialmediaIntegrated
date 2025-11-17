using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Models;
using SocialMediaAPI.Data;
using System.Security.Claims;
using System.Text.Json;
using System.Web;

namespace SocialMediaAPI.Controllers;

/// <summary>
/// Controller for Facebook OAuth 2.0 authentication and page management
/// Implements the complete OAuth flow for connecting Facebook Pages
/// </summary>
[ApiController]
[Route("api/facebook")]
public class FacebookAuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FacebookAuthController> _logger;

    public FacebookAuthController(
        IConfiguration configuration,
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<FacebookAuthController> logger)
    {
        _configuration = configuration;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Step 1: Get the Facebook OAuth authorization URL
    /// </summary>
    /// <remarks>
    /// Returns the URL to redirect the user to Facebook for authentication.
    /// The user must grant the required permissions for managing pages.
    /// </remarks>
    [HttpPost("oauth/authorize")]
    [Authorize]
    public ActionResult<FacebookAuthUrlResponse> GetAuthorizationUrl([FromBody] FacebookAuthUrlRequest request)
    {
        try
        {
            var appId = _configuration["Facebook:AppId"];
            var redirectUri = request.RedirectUri ?? _configuration["Facebook:RedirectUri"];
            var scopes = _configuration.GetSection("Facebook:Scopes").Get<string[]>();
            var state = request.State ?? Guid.NewGuid().ToString("N");

            if (string.IsNullOrEmpty(appId))
            {
                return BadRequest(new { message = "Facebook App ID is not configured" });
            }

            // Build the authorization URL
            var scopesString = string.Join(",", scopes ?? new[] { "pages_show_list", "pages_manage_posts", "pages_read_engagement" });
            var authUrl = $"https://www.facebook.com/v19.0/dialog/oauth?" +
                         $"client_id={appId}&" +
                         $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                         $"state={state}&" +
                         $"scope={HttpUtility.UrlEncode(scopesString)}";

            return Ok(new FacebookAuthUrlResponse
            {
                AuthorizationUrl = authUrl,
                State = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Facebook authorization URL");
            return StatusCode(500, new { message = "Failed to generate authorization URL", error = ex.Message });
        }
    }

    /// <summary>
    /// Step 2: Handle OAuth callback and exchange code for access token
    /// </summary>
    /// <remarks>
    /// After user authorizes the app on Facebook, they are redirected back with a code.
    /// This endpoint exchanges that code for an access token and retrieves the user's pages.
    /// </remarks>
    [HttpPost("oauth/callback")]
    [Authorize]
    public async Task<ActionResult<FacebookOAuthCallbackResponse>> HandleOAuthCallback([FromBody] FacebookOAuthCallbackRequest request)
    {
        try
        {
            var appId = _configuration["Facebook:AppId"];
            var appSecret = _configuration["Facebook:AppSecret"];
            var redirectUri = request.RedirectUri ?? _configuration["Facebook:RedirectUri"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret))
            {
                return BadRequest(new { message = "Facebook credentials are not configured" });
            }

            // Exchange authorization code for access token
            var httpClient = _httpClientFactory.CreateClient();
            var tokenUrl = $"https://graph.facebook.com/v19.0/oauth/access_token?" +
                          $"client_id={appId}&" +
                          $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                          $"client_secret={appSecret}&" +
                          $"code={request.Code}";

            var tokenResponse = await httpClient.GetAsync(tokenUrl);
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Facebook token exchange failed: {Content}", tokenContent);
                var errorResponse = JsonSerializer.Deserialize<FacebookErrorResponse>(tokenContent);
                return BadRequest(new { message = "Failed to exchange code for token", error = errorResponse?.Error.Message });
            }

            var tokenData = JsonSerializer.Deserialize<FacebookTokenResponse>(tokenContent);
            if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
            {
                return BadRequest(new { message = "Invalid token response from Facebook" });
            }

            // Get user's pages
            var pages = await GetUserPagesAsync(tokenData.AccessToken);

            return Ok(new FacebookOAuthCallbackResponse
            {
                AccessToken = tokenData.AccessToken,
                TokenType = tokenData.TokenType,
                ExpiresIn = tokenData.ExpiresIn ?? 3600,
                Pages = pages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Facebook OAuth callback");
            return StatusCode(500, new { message = "Failed to complete OAuth flow", error = ex.Message });
        }
    }

    /// <summary>
    /// Get list of Facebook Pages the user manages
    /// </summary>
    /// <param name="accessToken">User access token</param>
    [HttpGet("pages")]
    [Authorize]
    public async Task<ActionResult<List<FacebookPageInfo>>> GetPages([FromQuery] string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(new { message = "Access token is required" });
            }

            var pages = await GetUserPagesAsync(accessToken);
            return Ok(pages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Facebook pages");
            return StatusCode(500, new { message = "Failed to retrieve pages", error = ex.Message });
        }
    }

    /// <summary>
    /// Step 3: Connect a specific Facebook Page to the user's account
    /// </summary>
    /// <remarks>
    /// Stores the page credentials in the database for future posting.
    /// The page access token is long-lived and doesn't expire unless password is changed.
    /// </remarks>
    [HttpPost("pages/connect")]
    [Authorize]
    public async Task<ActionResult<SocialAccountResponse>> ConnectPage([FromBody] ConnectFacebookPageRequest request)
    {
        try
        {
            var userId = GetUserId();

            // Check if page is already connected
            var existingAccount = await _context.SocialAccounts
                .FirstOrDefaultAsync(sa => sa.UserId == userId &&
                                          sa.Platform == "Facebook" &&
                                          sa.PageId == request.PageId);

            if (existingAccount != null)
            {
                // Update existing connection
                existingAccount.PageAccessToken = request.PageAccessToken;
                existingAccount.PageName = request.PageName;
                existingAccount.Metadata = request.Metadata;
                existingAccount.IsActive = true;
                existingAccount.LastSyncedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new connection
                var socialAccount = new SocialAccount
                {
                    UserId = userId,
                    Platform = "Facebook",
                    AccountId = request.PageId,
                    AccountName = request.PageName,
                    PageId = request.PageId,
                    PageAccessToken = request.PageAccessToken,
                    PageName = request.PageName,
                    IsPageAccount = true,
                    Metadata = request.Metadata,
                    IsActive = true,
                    ConnectedAt = DateTime.UtcNow
                };

                _context.SocialAccounts.Add(socialAccount);
            }

            await _context.SaveChangesAsync();

            var account = existingAccount ?? await _context.SocialAccounts
                .FirstOrDefaultAsync(sa => sa.UserId == userId &&
                                          sa.Platform == "Facebook" &&
                                          sa.PageId == request.PageId);

            return Ok(new SocialAccountResponse
            {
                Id = account!.Id,
                Platform = account.Platform,
                AccountId = account.AccountId,
                AccountName = account.PageName,
                IsActive = account.IsActive,
                ConnectedAt = account.ConnectedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting Facebook page");
            return StatusCode(500, new { message = "Failed to connect Facebook page", error = ex.Message });
        }
    }

    /// <summary>
    /// Exchange short-lived token for long-lived token (60 days)
    /// </summary>
    [HttpPost("token/extend")]
    [Authorize]
    public async Task<ActionResult<FacebookTokenResponse>> ExtendToken([FromBody] string shortLivedToken)
    {
        try
        {
            var appId = _configuration["Facebook:AppId"];
            var appSecret = _configuration["Facebook:AppSecret"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret))
            {
                return BadRequest(new { message = "Facebook credentials are not configured" });
            }

            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://graph.facebook.com/v19.0/oauth/access_token?" +
                     $"grant_type=fb_exchange_token&" +
                     $"client_id={appId}&" +
                     $"client_secret={appSecret}&" +
                     $"fb_exchange_token={shortLivedToken}";

            var response = await httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<FacebookErrorResponse>(content);
                return BadRequest(new { message = "Failed to extend token", error = errorResponse?.Error.Message });
            }

            var tokenData = JsonSerializer.Deserialize<FacebookTokenResponse>(content);
            return Ok(tokenData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending Facebook token");
            return StatusCode(500, new { message = "Failed to extend token", error = ex.Message });
        }
    }

    #region Helper Methods

    /// <summary>
    /// Helper method to get user's Facebook Pages
    /// </summary>
    private async Task<List<FacebookPageInfo>> GetUserPagesAsync(string accessToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var pagesUrl = $"https://graph.facebook.com/v19.0/me/accounts?access_token={accessToken}";

        var pagesResponse = await httpClient.GetAsync(pagesUrl);
        var pagesContent = await pagesResponse.Content.ReadAsStringAsync();

        if (!pagesResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to retrieve Facebook pages: {Content}", pagesContent);
            throw new Exception("Failed to retrieve user's Facebook pages");
        }

        var accountsData = JsonSerializer.Deserialize<FacebookAccountsResponse>(pagesContent);
        if (accountsData == null || accountsData.Data == null)
        {
            return new List<FacebookPageInfo>();
        }

        return accountsData.Data.Select(page => new FacebookPageInfo
        {
            Id = page.Id,
            Name = page.Name,
            AccessToken = page.AccessToken,
            Category = page.Category,
            Tasks = page.Tasks ?? new List<string>()
        }).ToList();
    }

    /// <summary>
    /// Get the current user ID from JWT claims
    /// </summary>
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    #endregion
}
