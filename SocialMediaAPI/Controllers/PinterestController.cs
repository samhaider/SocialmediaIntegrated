using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;

namespace SocialMediaAPI.Controllers;

/// <summary>
/// Controller for Pinterest-specific operations
/// Provides endpoints for boards, pins, and analytics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PinterestController : ControllerBase
{
    private readonly IPinterestService _pinterestService;
    private readonly ISocialAccountService _socialAccountService;
    private readonly ILogger<PinterestController> _logger;

    public PinterestController(
        IPinterestService pinterestService,
        ISocialAccountService socialAccountService,
        ILogger<PinterestController> logger)
    {
        _pinterestService = pinterestService;
        _socialAccountService = socialAccountService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's ID from JWT claims
    /// </summary>
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    /// <summary>
    /// Gets the Pinterest access token for the current user
    /// </summary>
    private async Task<string?> GetPinterestAccessTokenAsync()
    {
        var userId = GetUserId();
        var accounts = await _socialAccountService.GetUserAccountsAsync(userId);
        var pinterestAccount = accounts.FirstOrDefault(a => a.Platform.Equals("Pinterest", StringComparison.OrdinalIgnoreCase));

        if (pinterestAccount == null)
        {
            return null;
        }

        // Check if token is expired and needs refresh
        if (pinterestAccount.TokenExpiresAt.HasValue &&
            pinterestAccount.TokenExpiresAt.Value <= DateTime.UtcNow &&
            !string.IsNullOrEmpty(pinterestAccount.RefreshToken))
        {
            _logger.LogInformation("Pinterest token expired, refreshing...");
            var tokenResponse = await _pinterestService.RefreshAccessTokenAsync(pinterestAccount.RefreshToken);

            if (tokenResponse != null)
            {
                // Update the stored token (you may want to implement an update method in ISocialAccountService)
                _logger.LogInformation("Pinterest token refreshed successfully");
                return tokenResponse.AccessToken;
            }
        }

        return pinterestAccount.AccessToken;
    }

    /// <summary>
    /// Gets the authenticated user's Pinterest account information
    /// GET api/pinterest/account
    /// </summary>
    [HttpGet("account")]
    public async Task<ActionResult<PinterestUserResponse>> GetAccount()
    {
        try
        {
            var accessToken = await GetPinterestAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                return NotFound(new { message = "Pinterest account not connected. Please connect your Pinterest account first." });
            }

            var userInfo = await _pinterestService.GetUserAccountAsync(accessToken);

            if (userInfo == null)
            {
                return BadRequest(new { message = "Failed to retrieve Pinterest account information" });
            }

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest account");
            return StatusCode(500, new { message = "An error occurred while retrieving Pinterest account information" });
        }
    }

    /// <summary>
    /// Lists all boards for the authenticated user
    /// GET api/pinterest/boards?pageSize=25&bookmark=...
    /// </summary>
    [HttpGet("boards")]
    public async Task<ActionResult<PinterestBoardsResponse>> GetBoards([FromQuery] string? pageSize = "25", [FromQuery] string? bookmark = null)
    {
        try
        {
            var accessToken = await GetPinterestAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                return NotFound(new { message = "Pinterest account not connected" });
            }

            var boards = await _pinterestService.GetBoardsAsync(accessToken, pageSize, bookmark);

            if (boards == null)
            {
                return BadRequest(new { message = "Failed to retrieve Pinterest boards" });
            }

            return Ok(boards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest boards");
            return StatusCode(500, new { message = "An error occurred while retrieving Pinterest boards" });
        }
    }

    /// <summary>
    /// Creates a new pin on a specified board
    /// POST api/pinterest/pins
    /// </summary>
    [HttpPost("pins")]
    public async Task<ActionResult<PinterestPin>> CreatePin([FromBody] CreatePinterestPinRequest request)
    {
        try
        {
            var accessToken = await GetPinterestAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                return NotFound(new { message = "Pinterest account not connected" });
            }

            // Validate request
            if (string.IsNullOrEmpty(request.BoardId))
            {
                return BadRequest(new { message = "BoardId is required" });
            }

            if (string.IsNullOrEmpty(request.MediaUrl))
            {
                return BadRequest(new { message = "MediaUrl is required" });
            }

            // Create Pinterest API request
            var createPinRequest = new PinterestCreatePinRequest
            {
                BoardId = request.BoardId,
                BoardSectionId = request.BoardSectionId,
                Title = request.Title,
                Description = request.Description,
                Link = request.Link,
                AltText = request.AltText,
                MediaSource = new PinterestMediaSource
                {
                    SourceType = request.MediaType,
                    Url = request.MediaUrl
                }
            };

            var pin = await _pinterestService.CreatePinAsync(accessToken, createPinRequest);

            if (pin == null)
            {
                return BadRequest(new { message = "Failed to create Pinterest pin" });
            }

            return CreatedAtAction(nameof(GetPin), new { pinId = pin.Id }, pin);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating Pinterest pin");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Pinterest pin");
            return StatusCode(500, new { message = "An error occurred while creating the Pinterest pin" });
        }
    }

    /// <summary>
    /// Gets details of a specific pin
    /// GET api/pinterest/pins/{pinId}
    /// </summary>
    [HttpGet("pins/{pinId}")]
    public async Task<ActionResult<PinterestPin>> GetPin(string pinId)
    {
        try
        {
            var accessToken = await GetPinterestAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                return NotFound(new { message = "Pinterest account not connected" });
            }

            var pin = await _pinterestService.GetPinAsync(accessToken, pinId);

            if (pin == null)
            {
                return NotFound(new { message = $"Pin with ID {pinId} not found" });
            }

            return Ok(pin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest pin {PinId}", pinId);
            return StatusCode(500, new { message = "An error occurred while retrieving the Pinterest pin" });
        }
    }

    /// <summary>
    /// Lists all pins for the authenticated user
    /// GET api/pinterest/pins?pageSize=25&bookmark=...
    /// </summary>
    [HttpGet("pins")]
    public async Task<ActionResult<PinterestPinsResponse>> GetUserPins([FromQuery] string? pageSize = "25", [FromQuery] string? bookmark = null)
    {
        try
        {
            var accessToken = await GetPinterestAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                return NotFound(new { message = "Pinterest account not connected" });
            }

            var pins = await _pinterestService.GetUserPinsAsync(accessToken, pageSize, bookmark);

            if (pins == null)
            {
                return BadRequest(new { message = "Failed to retrieve Pinterest pins" });
            }

            return Ok(pins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest user pins");
            return StatusCode(500, new { message = "An error occurred while retrieving Pinterest pins" });
        }
    }

    /// <summary>
    /// Lists pins on a specific board
    /// GET api/pinterest/boards/{boardId}/pins?pageSize=25&bookmark=...
    /// </summary>
    [HttpGet("boards/{boardId}/pins")]
    public async Task<ActionResult<PinterestPinsResponse>> GetBoardPins(string boardId, [FromQuery] string? pageSize = "25", [FromQuery] string? bookmark = null)
    {
        try
        {
            var accessToken = await GetPinterestAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                return NotFound(new { message = "Pinterest account not connected" });
            }

            var pins = await _pinterestService.GetBoardPinsAsync(accessToken, boardId, pageSize, bookmark);

            if (pins == null)
            {
                return BadRequest(new { message = "Failed to retrieve board pins" });
            }

            return Ok(pins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest board pins for board {BoardId}", boardId);
            return StatusCode(500, new { message = "An error occurred while retrieving board pins" });
        }
    }

    /// <summary>
    /// Gets analytics for a specific pin
    /// GET api/pinterest/pins/{pinId}/analytics?startDate=2025-01-01&endDate=2025-01-31
    /// </summary>
    [HttpGet("pins/{pinId}/analytics")]
    public async Task<ActionResult<PinterestPinAnalytics>> GetPinAnalytics(
        string pinId,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            var accessToken = await GetPinterestAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                return NotFound(new { message = "Pinterest account not connected" });
            }

            // Default to last 30 days if dates not provided
            var end = string.IsNullOrEmpty(endDate) ? DateTime.UtcNow.ToString("yyyy-MM-dd") : endDate;
            var start = string.IsNullOrEmpty(startDate) ? DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd") : startDate;

            var analytics = await _pinterestService.GetPinAnalyticsAsync(accessToken, pinId, start, end);

            if (analytics == null)
            {
                return NotFound(new { message = "Analytics not available for this pin" });
            }

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest pin analytics for pin {PinId}", pinId);
            return StatusCode(500, new { message = "An error occurred while retrieving pin analytics" });
        }
    }

    /// <summary>
    /// Deletes a pin
    /// DELETE api/pinterest/pins/{pinId}
    /// </summary>
    [HttpDelete("pins/{pinId}")]
    public async Task<ActionResult> DeletePin(string pinId)
    {
        try
        {
            var accessToken = await GetPinterestAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                return NotFound(new { message = "Pinterest account not connected" });
            }

            var success = await _pinterestService.DeletePinAsync(accessToken, pinId);

            if (!success)
            {
                return BadRequest(new { message = "Failed to delete Pinterest pin" });
            }

            return Ok(new { message = "Pin deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Pinterest pin {PinId}", pinId);
            return StatusCode(500, new { message = "An error occurred while deleting the Pinterest pin" });
        }
    }

    /// <summary>
    /// OAuth callback endpoint - receives authorization code and exchanges for tokens
    /// This would typically be called by Pinterest's OAuth redirect
    /// GET api/pinterest/oauth/callback?code=...&state=...
    /// </summary>
    [HttpGet("oauth/callback")]
    [AllowAnonymous]
    public async Task<ActionResult> OAuthCallback([FromQuery] string code, [FromQuery] string? state = null)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { message = "Authorization code is required" });
            }

            // Note: In a production app, you would:
            // 1. Exchange the authorization code for access/refresh tokens
            // 2. Associate the tokens with the authenticated user
            // 3. Store the tokens securely in the database
            // 4. Redirect the user back to your application

            _logger.LogInformation("Received Pinterest OAuth callback with code");

            return Ok(new
            {
                message = "OAuth callback received. Please implement token exchange logic in production.",
                code = code,
                state = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Pinterest OAuth callback");
            return StatusCode(500, new { message = "An error occurred during OAuth authentication" });
        }
    }
}
