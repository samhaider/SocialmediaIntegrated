using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Services.LinkedIn;
using System.Security.Claims;

namespace SocialMediaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LinkedInController : ControllerBase
{
    private readonly ILinkedInOAuthService _oauthService;
    private readonly ILogger<LinkedInController> _logger;

    public LinkedInController(
        ILinkedInOAuthService oauthService,
        ILogger<LinkedInController> logger)
    {
        _oauthService = oauthService;
        _logger = logger;
    }

    /// <summary>
    /// Get the LinkedIn authorization URL to start the OAuth flow
    /// </summary>
    /// <returns>Authorization URL that the user should visit</returns>
    [HttpGet("auth/url")]
    public IActionResult GetAuthorizationUrl([FromQuery] string? state = null)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var authState = state ?? $"{userId}_{Guid.NewGuid()}";

            var authUrl = _oauthService.GetAuthorizationUrl(authState);

            return Ok(new
            {
                authorizationUrl = authUrl,
                state = authState,
                instructions = new
                {
                    step1 = "Visit the authorization URL",
                    step2 = "Grant permissions to your LinkedIn account or organization",
                    step3 = "You will be redirected back with an authorization code",
                    step4 = "Use the /api/linkedin/auth/callback endpoint with the code to complete the connection"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LinkedIn authorization URL");
            return StatusCode(500, new { error = "Failed to generate authorization URL" });
        }
    }

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    /// <param name="code">Authorization code from LinkedIn</param>
    /// <returns>Access token response</returns>
    [HttpPost("auth/callback")]
    public async Task<IActionResult> HandleCallback([FromBody] LinkedInCallbackRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Code))
            {
                return BadRequest(new { error = "Authorization code is required" });
            }

            var tokenResponse = await _oauthService.ExchangeCodeForTokenAsync(request.Code);

            return Ok(new
            {
                accessToken = tokenResponse.AccessToken,
                expiresIn = tokenResponse.ExpiresIn,
                refreshToken = tokenResponse.RefreshToken,
                scope = tokenResponse.Scope,
                nextSteps = new
                {
                    step1 = "Use the access token to get your organizations (optional)",
                    step2 = "Connect the account using /api/socialaccounts endpoint",
                    note = "For organization posting, include the organizationId in the connect request"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code");
            return StatusCode(500, new { error = "Failed to exchange authorization code" });
        }
    }

    /// <summary>
    /// Get user information from LinkedIn
    /// </summary>
    /// <param name="accessToken">LinkedIn access token</param>
    /// <returns>User information</returns>
    [HttpGet("user/info")]
    public async Task<IActionResult> GetUserInfo([FromQuery] string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(new { error = "Access token is required" });
            }

            var userInfo = await _oauthService.GetUserInfoAsync(accessToken);

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LinkedIn user info");
            return StatusCode(500, new { error = "Failed to get user information" });
        }
    }

    /// <summary>
    /// Get organizations (company pages) that the user has access to
    /// </summary>
    /// <param name="accessToken">LinkedIn access token with w_organization_social scope</param>
    /// <returns>List of organizations with their roles</returns>
    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations([FromQuery] string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(new { error = "Access token is required" });
            }

            var organizations = await _oauthService.GetUserOrganizationsAsync(accessToken);

            if (!organizations.Any())
            {
                return Ok(new
                {
                    organizations = new List<object>(),
                    message = "No organizations found. Make sure you:",
                    requirements = new[]
                    {
                        "Have ADMINISTRATOR or CONTENT_ADMIN role on a LinkedIn Company Page",
                        "Granted the w_organization_social scope during authorization",
                        "Your LinkedIn app has access to the Marketing Developer Platform"
                    }
                });
            }

            var organizationList = organizations.Select(org => new
            {
                organizationUrn = org.Organization,
                organizationName = org.OrganizationName,
                role = org.Role,
                state = org.State,
                canPost = org.Role is "ADMINISTRATOR" or "CONTENT_ADMIN" or "DIRECT_SPONSORED_CONTENT_POSTER"
            }).ToList();

            return Ok(new
            {
                count = organizationList.Count,
                organizations = organizationList,
                instructions = new
                {
                    message = "To post as an organization, use the organizationUrn when connecting the account",
                    example = "Set AccountType='Organization' and OrganizationId to the organizationUrn"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LinkedIn organizations");
            return StatusCode(500, new { error = "Failed to get organizations" });
        }
    }

    /// <summary>
    /// Refresh an expired access token
    /// </summary>
    /// <param name="refreshToken">LinkedIn refresh token</param>
    /// <returns>New access token</returns>
    [HttpPost("auth/refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] LinkedInRefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { error = "Refresh token is required" });
            }

            var tokenResponse = await _oauthService.RefreshTokenAsync(request.RefreshToken);

            return Ok(new
            {
                accessToken = tokenResponse.AccessToken,
                expiresIn = tokenResponse.ExpiresIn,
                refreshToken = tokenResponse.RefreshToken,
                scope = tokenResponse.Scope
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing LinkedIn token");
            return StatusCode(500, new { error = "Failed to refresh token" });
        }
    }
}

// Request DTOs for LinkedIn controller
public class LinkedInCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string? State { get; set; }
}

public class LinkedInRefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
