using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using SocialMediaAPI.Configuration;
using SocialMediaAPI.Data;
using SocialMediaAPI.DTOs.Instagram;
using SocialMediaAPI.Models;
using SocialMediaAPI.Services.Instagram;
using Microsoft.EntityFrameworkCore;

namespace SocialMediaAPI.Controllers;

/// <summary>
/// Controller for Instagram OAuth authentication and account management
/// </summary>
[ApiController]
[Route("api/instagram")]
public class InstagramOAuthController : ControllerBase
{
    private readonly IInstagramGraphApiService _instagramService;
    private readonly InstagramSettings _settings;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InstagramOAuthController> _logger;

    public InstagramOAuthController(
        IInstagramGraphApiService instagramService,
        IOptions<InstagramSettings> settings,
        ApplicationDbContext context,
        ILogger<InstagramOAuthController> logger)
    {
        _instagramService = instagramService;
        _settings = settings.Value;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get Facebook OAuth authorization URL
    /// </summary>
    /// <returns>Authorization URL for user to authenticate</returns>
    [HttpGet("auth/url")]
    [Authorize]
    public IActionResult GetAuthorizationUrl()
    {
        var scope = string.Join(",", _settings.RequiredPermissions);
        var state = Guid.NewGuid().ToString(); // Use this to prevent CSRF attacks

        var authUrl = $"{_settings.OAuthBaseUrl}" +
                     $"?client_id={_settings.AppId}" +
                     $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}" +
                     $"&scope={Uri.EscapeDataString(scope)}" +
                     $"&response_type=code" +
                     $"&state={state}";

        return Ok(new
        {
            authorizationUrl = authUrl,
            state = state,
            redirectUri = _settings.RedirectUri
        });
    }

    /// <summary>
    /// OAuth callback endpoint - Exchange code for access token
    /// </summary>
    /// <param name="code">Authorization code from Facebook</param>
    /// <param name="state">State parameter for CSRF protection</param>
    [HttpGet("auth/callback")]
    [Authorize]
    public async Task<IActionResult> HandleCallback([FromQuery] string code, [FromQuery] string? state)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { error = "Authorization code is required" });
            }

            var userId = GetUserId();

            // Step 1: Exchange code for short-lived token
            var tokenResponse = await _instagramService.ExchangeCodeForTokenAsync(code);

            // Step 2: Convert to long-lived token (60 days)
            var longLivedToken = await _instagramService.GetLongLivedTokenAsync(tokenResponse.AccessToken);

            // Step 3: Get Facebook Pages connected to user
            var pages = await _instagramService.GetUserPagesAsync(longLivedToken.AccessToken);

            // Step 4: Find pages with Instagram Business Accounts
            var connectedAccounts = new List<object>();

            foreach (var page in pages)
            {
                if (page.InstagramBusinessAccount != null && !string.IsNullOrEmpty(page.AccessToken))
                {
                    // Get detailed Instagram account info
                    var igAccount = await _instagramService.GetInstagramBusinessAccountAsync(
                        page.Id,
                        page.AccessToken);

                    if (igAccount != null)
                    {
                        // Save or update social account
                        var socialAccount = await _context.SocialAccounts
                            .FirstOrDefaultAsync(sa =>
                                sa.UserId == userId &&
                                sa.Platform == "Instagram" &&
                                sa.AccountId == igAccount.Id);

                        if (socialAccount == null)
                        {
                            socialAccount = new SocialAccount
                            {
                                UserId = userId,
                                Platform = "Instagram",
                                AccountId = igAccount.Id,
                                AccountName = igAccount.Username ?? igAccount.Name ?? igAccount.Id,
                                AccessToken = page.AccessToken, // Use page access token
                                RefreshToken = longLivedToken.AccessToken, // Store user token for refresh
                                TokenExpiresAt = DateTime.UtcNow.AddSeconds(longLivedToken.ExpiresIn),
                                IsActive = true,
                                ConnectedAt = DateTime.UtcNow
                            };
                            _context.SocialAccounts.Add(socialAccount);
                        }
                        else
                        {
                            socialAccount.AccessToken = page.AccessToken;
                            socialAccount.RefreshToken = longLivedToken.AccessToken;
                            socialAccount.TokenExpiresAt = DateTime.UtcNow.AddSeconds(longLivedToken.ExpiresIn);
                            socialAccount.IsActive = true;
                            socialAccount.LastSyncedAt = DateTime.UtcNow;
                        }

                        await _context.SaveChangesAsync();

                        connectedAccounts.Add(new
                        {
                            id = socialAccount.Id,
                            instagramAccountId = igAccount.Id,
                            username = igAccount.Username,
                            name = igAccount.Name,
                            profilePictureUrl = igAccount.ProfilePictureUrl,
                            followersCount = igAccount.FollowersCount,
                            followsCount = igAccount.FollowsCount,
                            mediaCount = igAccount.MediaCount,
                            facebookPageId = page.Id,
                            facebookPageName = page.Name,
                            tokenExpiresAt = socialAccount.TokenExpiresAt
                        });
                    }
                }
            }

            if (connectedAccounts.Count == 0)
            {
                return Ok(new
                {
                    success = false,
                    message = "No Instagram Business Accounts found. Please ensure your Instagram account is a Business or Creator account and is connected to a Facebook Page."
                });
            }

            return Ok(new
            {
                success = true,
                message = $"Successfully connected {connectedAccounts.Count} Instagram account(s)",
                accounts = connectedAccounts
            });
        }
        catch (InstagramApiException ex)
        {
            _logger.LogError(ex, "Instagram API error during OAuth callback");
            return BadRequest(new
            {
                error = "Instagram API error",
                message = ex.Message,
                code = ex.ErrorCode,
                subcode = ex.ErrorSubcode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth callback");
            return StatusCode(500, new { error = "Failed to connect Instagram account", details = ex.Message });
        }
    }

    /// <summary>
    /// Refresh access token for an Instagram account
    /// </summary>
    /// <param name="accountId">Social account ID</param>
    [HttpPost("accounts/{accountId}/refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken(int accountId)
    {
        try
        {
            var userId = GetUserId();

            var account = await _context.SocialAccounts
                .FirstOrDefaultAsync(sa =>
                    sa.Id == accountId &&
                    sa.UserId == userId &&
                    sa.Platform == "Instagram");

            if (account == null)
            {
                return NotFound(new { error = "Instagram account not found" });
            }

            if (string.IsNullOrEmpty(account.RefreshToken))
            {
                return BadRequest(new { error = "No refresh token available. Please reconnect the account." });
            }

            // Refresh the long-lived token
            var refreshedToken = await _instagramService.RefreshLongLivedTokenAsync(account.RefreshToken);

            // Debug token to verify
            var tokenInfo = await _instagramService.DebugTokenAsync(refreshedToken.AccessToken);

            if (!tokenInfo.IsValid)
            {
                return BadRequest(new { error = "Refreshed token is not valid. Please reconnect the account." });
            }

            // Update account
            account.RefreshToken = refreshedToken.AccessToken;
            account.TokenExpiresAt = DateTime.UtcNow.AddSeconds(refreshedToken.ExpiresIn);
            account.LastSyncedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Token refreshed successfully",
                expiresAt = account.TokenExpiresAt,
                expiresIn = refreshedToken.ExpiresIn
            });
        }
        catch (InstagramApiException ex)
        {
            _logger.LogError(ex, "Instagram API error during token refresh");
            return BadRequest(new
            {
                error = "Failed to refresh token",
                message = ex.Message,
                code = ex.ErrorCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Instagram token for account {AccountId}", accountId);
            return StatusCode(500, new { error = "Failed to refresh token", details = ex.Message });
        }
    }

    /// <summary>
    /// Get Instagram account details
    /// </summary>
    /// <param name="accountId">Social account ID</param>
    [HttpGet("accounts/{accountId}")]
    [Authorize]
    public async Task<IActionResult> GetAccountDetails(int accountId)
    {
        try
        {
            var userId = GetUserId();

            var account = await _context.SocialAccounts
                .FirstOrDefaultAsync(sa =>
                    sa.Id == accountId &&
                    sa.UserId == userId &&
                    sa.Platform == "Instagram");

            if (account == null)
            {
                return NotFound(new { error = "Instagram account not found" });
            }

            // Check if token is about to expire
            var shouldRefresh = account.TokenExpiresAt.HasValue &&
                              account.TokenExpiresAt.Value.AddDays(-_settings.TokenRefreshThresholdDays) <= DateTime.UtcNow;

            return Ok(new
            {
                id = account.Id,
                accountId = account.AccountId,
                accountName = account.AccountName,
                isActive = account.IsActive,
                connectedAt = account.ConnectedAt,
                lastSyncedAt = account.LastSyncedAt,
                tokenExpiresAt = account.TokenExpiresAt,
                shouldRefreshToken = shouldRefresh
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Instagram account details for {AccountId}", accountId);
            return StatusCode(500, new { error = "Failed to get account details", details = ex.Message });
        }
    }

    /// <summary>
    /// Disconnect Instagram account
    /// </summary>
    /// <param name="accountId">Social account ID</param>
    [HttpDelete("accounts/{accountId}")]
    [Authorize]
    public async Task<IActionResult> DisconnectAccount(int accountId)
    {
        try
        {
            var userId = GetUserId();

            var account = await _context.SocialAccounts
                .FirstOrDefaultAsync(sa =>
                    sa.Id == accountId &&
                    sa.UserId == userId &&
                    sa.Platform == "Instagram");

            if (account == null)
            {
                return NotFound(new { error = "Instagram account not found" });
            }

            _context.SocialAccounts.Remove(account);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Instagram account disconnected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting Instagram account {AccountId}", accountId);
            return StatusCode(500, new { error = "Failed to disconnect account", details = ex.Message });
        }
    }

    #region Helper Methods

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    #endregion
}
