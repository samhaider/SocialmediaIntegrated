using Microsoft.Extensions.Configuration;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Services.Platforms;

/// <summary>
/// Pinterest API v5 integration platform implementation
/// Supports OAuth 2.0, pin creation, board management, and analytics
/// </summary>
public class PinterestPlatform : ISocialMediaPlatform
{
    private readonly IPinterestService _pinterestService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PinterestPlatform> _logger;

    public string PlatformName => "Pinterest";

    public PinterestPlatform(
        IPinterestService pinterestService,
        IConfiguration configuration,
        ILogger<PinterestPlatform> logger)
    {
        _pinterestService = pinterestService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Validates Pinterest access token by fetching user account info
    /// </summary>
    public async Task<bool> ValidateConnectionAsync(string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
                return false;

            return await _pinterestService.ValidateAccessTokenAsync(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Pinterest connection");
            return false;
        }
    }

    /// <summary>
    /// Publishes a post as a Pinterest pin
    /// Requires at least one media URL and creates pin on the default board
    /// </summary>
    public async Task<string> PublishPostAsync(string accessToken, string content, List<string>? mediaUrls)
    {
        try
        {
            // Pinterest requires at least one image/video
            if (mediaUrls == null || mediaUrls.Count == 0)
            {
                throw new InvalidOperationException("Pinterest requires at least one image or video URL");
            }

            // Get user's boards to find a default board
            var boardsResponse = await _pinterestService.GetBoardsAsync(accessToken, pageSize: "1");

            if (boardsResponse == null || boardsResponse.Items.Count == 0)
            {
                throw new InvalidOperationException("No Pinterest boards found. Please create a board first.");
            }

            var defaultBoard = boardsResponse.Items[0];
            _logger.LogInformation("Using default board: {BoardName} (ID: {BoardId})", defaultBoard.Name, defaultBoard.Id);

            // Create pin request
            var createPinRequest = new PinterestCreatePinRequest
            {
                BoardId = defaultBoard.Id,
                Title = TruncateString(content, 100), // Pinterest title max 100 chars
                Description = content,
                Link = null, // Optional: could be extracted from content or mediaUrls
                MediaSource = new PinterestMediaSource
                {
                    SourceType = "image_url",
                    Url = mediaUrls[0]
                }
            };

            var pin = await _pinterestService.CreatePinAsync(accessToken, createPinRequest);

            if (pin == null || string.IsNullOrEmpty(pin.Id))
            {
                throw new InvalidOperationException("Failed to create Pinterest pin");
            }

            _logger.LogInformation("Successfully created Pinterest pin: {PinId}", pin.Id);
            return pin.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing post to Pinterest");
            throw;
        }
    }

    /// <summary>
    /// Gets analytics for a Pinterest pin
    /// </summary>
    public async Task<PostAnalytics> GetPostAnalyticsAsync(string accessToken, string platformPostId)
    {
        try
        {
            // Get analytics for the last 30 days
            var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");

            var analytics = await _pinterestService.GetPinAnalyticsAsync(accessToken, platformPostId, startDate, endDate);

            if (analytics == null)
            {
                _logger.LogWarning("No analytics available for Pinterest pin {PinId}", platformPostId);
                return new PostAnalytics
                {
                    Likes = 0,
                    Comments = 0,
                    Shares = 0,
                    Views = 0,
                    Clicks = 0
                };
            }

            // Map Pinterest analytics to PostAnalytics model
            return new PostAnalytics
            {
                Likes = analytics.Saves ?? 0,              // Pinterest "Saves" = Likes
                Comments = 0,                              // Pinterest API v5 doesn't provide comment counts in analytics
                Shares = analytics.PinClicks ?? 0,        // Pinterest "Pin clicks" = Shares (repins)
                Views = analytics.Impressions ?? 0,        // Impressions = Views
                Clicks = analytics.OutboundClicks ?? 0     // Outbound clicks
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest analytics for pin {PinId}", platformPostId);

            // Return empty analytics on error
            return new PostAnalytics
            {
                Likes = 0,
                Comments = 0,
                Shares = 0,
                Views = 0,
                Clicks = 0
            };
        }
    }

    /// <summary>
    /// Gets comments for a Pinterest pin
    /// Note: Pinterest API v5 does not provide comment fetching functionality
    /// </summary>
    public Task<List<Comment>> GetCommentsAsync(string accessToken, string platformPostId)
    {
        _logger.LogWarning("Pinterest API v5 does not support fetching comments");
        return Task.FromResult(new List<Comment>());
    }

    /// <summary>
    /// Replies to a comment on Pinterest
    /// Note: Pinterest API v5 does not provide comment reply functionality
    /// </summary>
    public Task<bool> ReplyToCommentAsync(string accessToken, string commentId, string reply)
    {
        _logger.LogWarning("Pinterest API v5 does not support replying to comments");
        return Task.FromResult(false);
    }

    /// <summary>
    /// Gets direct messages from Pinterest
    /// Note: Pinterest API v5 does not provide direct messaging functionality
    /// </summary>
    public Task<List<DirectMessage>> GetDirectMessagesAsync(string accessToken)
    {
        _logger.LogWarning("Pinterest API v5 does not support direct messages");
        return Task.FromResult(new List<DirectMessage>());
    }

    /// <summary>
    /// Helper method to truncate strings to a maximum length
    /// </summary>
    private string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
