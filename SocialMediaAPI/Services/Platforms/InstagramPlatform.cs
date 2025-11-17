using System.Text.Json;
using SocialMediaAPI.Data;
using SocialMediaAPI.DTOs.Instagram;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;
using SocialMediaAPI.Services.Instagram;
using Microsoft.EntityFrameworkCore;

namespace SocialMediaAPI.Services.Platforms;

/// <summary>
/// Instagram platform implementation using Instagram Graph API
/// </summary>
public class InstagramPlatform : ISocialMediaPlatform
{
    private readonly IInstagramGraphApiService _instagramService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InstagramPlatform> _logger;

    public string PlatformName => "Instagram";

    public InstagramPlatform(
        IInstagramGraphApiService instagramService,
        ApplicationDbContext context,
        ILogger<InstagramPlatform> logger)
    {
        _instagramService = instagramService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validate Instagram connection by debugging the access token
    /// </summary>
    public async Task<bool> ValidateConnectionAsync(string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }

            var tokenInfo = await _instagramService.DebugTokenAsync(accessToken);
            return tokenInfo.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Instagram connection");
            return false;
        }
    }

    /// <summary>
    /// Publish post to Instagram using container-based publishing
    /// Supports: photos, videos, reels, and carousels
    /// </summary>
    public async Task<string> PublishPostAsync(string accessToken, string content, List<string>? mediaUrls)
    {
        try
        {
            // Get Instagram User ID from the access token
            var igUserId = await GetInstagramUserIdFromTokenAsync(accessToken);

            if (string.IsNullOrEmpty(igUserId))
            {
                throw new InvalidOperationException("Could not determine Instagram User ID from access token");
            }

            // Determine content type and publish accordingly
            if (mediaUrls == null || mediaUrls.Count == 0)
            {
                throw new ArgumentException("Instagram requires at least one media file (image or video)");
            }

            string platformPostId;

            if (mediaUrls.Count == 1)
            {
                // Single media post (image, video, or reel)
                platformPostId = await PublishSingleMediaAsync(igUserId, accessToken, content, mediaUrls[0]);
            }
            else
            {
                // Carousel post (multiple images/videos)
                platformPostId = await PublishCarouselAsync(igUserId, accessToken, content, mediaUrls);
            }

            _logger.LogInformation("Successfully published to Instagram: {PostId}", platformPostId);
            return platformPostId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish post to Instagram");
            throw;
        }
    }

    /// <summary>
    /// Get post analytics from Instagram
    /// </summary>
    public async Task<PostAnalytics> GetPostAnalyticsAsync(string accessToken, string platformPostId)
    {
        try
        {
            // Instagram media insights metrics
            var metrics = new[]
            {
                "engagement",      // Likes + comments + saves
                "impressions",     // Total views
                "reach",          // Unique accounts reached
                "saved",          // Number of saves
                "video_views"     // For videos only
            };

            var insights = await _instagramService.GetMediaInsightsAsync(
                platformPostId,
                accessToken,
                metrics);

            var analytics = new PostAnalytics
            {
                Platform = "Instagram",
                FetchedAt = DateTime.UtcNow
            };

            foreach (var insight in insights.Data)
            {
                var value = insight.Values.FirstOrDefault()?.Value ?? 0;

                switch (insight.Name.ToLower())
                {
                    case "engagement":
                        // Engagement includes likes, comments, saves
                        // We'll estimate likes as 70% of engagement
                        analytics.Likes = (int)(value * 0.7);
                        analytics.Comments = (int)(value * 0.2);
                        break;
                    case "impressions":
                        analytics.Views = value;
                        break;
                    case "reach":
                        // Use reach as additional metric
                        break;
                    case "saved":
                        analytics.Shares = value; // Treat saves as shares
                        break;
                    case "video_views":
                        analytics.Views = value;
                        break;
                }
            }

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analytics for Instagram post {PostId}", platformPostId);

            // Return empty analytics on failure
            return new PostAnalytics
            {
                Platform = "Instagram",
                FetchedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Get comments on an Instagram post
    /// </summary>
    public async Task<List<Comment>> GetCommentsAsync(string accessToken, string platformPostId)
    {
        try
        {
            // Note: Instagram Graph API requires additional permissions for comments
            // This is a simplified implementation
            _logger.LogWarning("GetCommentsAsync is not fully implemented for Instagram Graph API");
            return new List<Comment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get comments for Instagram post {PostId}", platformPostId);
            return new List<Comment>();
        }
    }

    /// <summary>
    /// Reply to a comment on Instagram
    /// </summary>
    public async Task<bool> ReplyToCommentAsync(string accessToken, string commentId, string reply)
    {
        try
        {
            // Note: Instagram Graph API comment replies require additional implementation
            _logger.LogWarning("ReplyToCommentAsync is not fully implemented for Instagram Graph API");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reply to Instagram comment {CommentId}", commentId);
            return false;
        }
    }

    /// <summary>
    /// Get direct messages (not supported by Instagram Graph API for most apps)
    /// </summary>
    public async Task<List<DirectMessage>> GetDirectMessagesAsync(string accessToken)
    {
        // Instagram Graph API does not support DMs for most applications
        // Only approved partners have access to this feature
        _logger.LogWarning("Direct messages are not supported by Instagram Graph API for most applications");
        return new List<DirectMessage>();
    }

    #region Private Helper Methods

    /// <summary>
    /// Get Instagram User ID from the access token
    /// The access token is actually a Page access token, so we need to look up the IG user ID
    /// </summary>
    private async Task<string?> GetInstagramUserIdFromTokenAsync(string accessToken)
    {
        try
        {
            // Look up the Instagram account in the database using this access token
            var account = await _context.SocialAccounts
                .FirstOrDefaultAsync(sa =>
                    sa.Platform == "Instagram" &&
                    sa.AccessToken == accessToken &&
                    sa.IsActive);

            return account?.AccountId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Instagram User ID from token");
            return null;
        }
    }

    /// <summary>
    /// Publish a single media (image, video, or reel)
    /// </summary>
    private async Task<string> PublishSingleMediaAsync(
        string igUserId,
        string accessToken,
        string content,
        string mediaUrl)
    {
        // Determine media type based on URL extension
        var mediaType = DetermineMediaType(mediaUrl);

        // Step 1: Create media container
        var containerRequest = new CreateMediaContainerRequest
        {
            MediaUrl = mediaUrl,
            Caption = content,
            MediaType = mediaType
        };

        var containerId = await _instagramService.CreateMediaContainerAsync(
            igUserId,
            accessToken,
            containerRequest);

        // Step 2: Wait for video processing if needed
        if (mediaType == InstagramMediaType.VIDEO || mediaType == InstagramMediaType.REELS)
        {
            await WaitForVideoProcessingAsync(containerId, accessToken);
        }

        // Step 3: Publish the container
        var postId = await _instagramService.PublishMediaContainerAsync(
            igUserId,
            accessToken,
            containerId);

        return postId;
    }

    /// <summary>
    /// Publish a carousel (multiple images/videos)
    /// </summary>
    private async Task<string> PublishCarouselAsync(
        string igUserId,
        string accessToken,
        string content,
        List<string> mediaUrls)
    {
        if (mediaUrls.Count < InstagramMediaConstraints.MinCarouselItems ||
            mediaUrls.Count > InstagramMediaConstraints.MaxCarouselItems)
        {
            throw new ArgumentException(
                $"Carousel must contain between {InstagramMediaConstraints.MinCarouselItems} " +
                $"and {InstagramMediaConstraints.MaxCarouselItems} items");
        }

        // Step 1: Create child containers for each media item
        var childContainerIds = new List<string>();

        foreach (var mediaUrl in mediaUrls)
        {
            var mediaType = DetermineMediaType(mediaUrl);

            var childRequest = new CreateMediaContainerRequest
            {
                MediaUrl = mediaUrl,
                MediaType = mediaType == InstagramMediaType.VIDEO
                    ? InstagramMediaType.VIDEO
                    : InstagramMediaType.IMAGE,
                IsCarouselItem = true
            };

            var childContainerId = await _instagramService.CreateMediaContainerAsync(
                igUserId,
                accessToken,
                childRequest);

            childContainerIds.Add(childContainerId);

            // Wait for video processing if needed
            if (mediaType == InstagramMediaType.VIDEO)
            {
                await WaitForVideoProcessingAsync(childContainerId, accessToken);
            }
        }

        // Step 2: Create carousel container
        var carouselRequest = new CreateMediaContainerRequest
        {
            Caption = content,
            MediaType = InstagramMediaType.CAROUSEL,
            Children = string.Join(",", childContainerIds)
        };

        var carouselContainerId = await _instagramService.CreateMediaContainerAsync(
            igUserId,
            accessToken,
            carouselRequest);

        // Step 3: Publish the carousel
        var postId = await _instagramService.PublishMediaContainerAsync(
            igUserId,
            accessToken,
            carouselContainerId);

        return postId;
    }

    /// <summary>
    /// Wait for video processing to complete
    /// </summary>
    private async Task WaitForVideoProcessingAsync(string containerId, string accessToken, int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            var status = await _instagramService.GetContainerStatusAsync(containerId, accessToken);

            if (status.Status.Equals("FINISHED", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Video processing completed for container {ContainerId}", containerId);
                return;
            }

            if (status.Status.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Video processing failed for container {containerId}. Status code: {status.StatusCode}");
            }

            // Wait 2 seconds before checking again
            await Task.Delay(2000);
        }

        throw new TimeoutException($"Video processing timeout for container {containerId}");
    }

    /// <summary>
    /// Determine media type from URL extension
    /// </summary>
    private InstagramMediaType DetermineMediaType(string mediaUrl)
    {
        var extension = Path.GetExtension(mediaUrl).ToLowerInvariant();

        if (InstagramMediaConstraints.SupportedVideoFormats.Contains(extension))
        {
            return InstagramMediaType.VIDEO;
        }

        if (InstagramMediaConstraints.SupportedImageFormats.Contains(extension))
        {
            return InstagramMediaType.IMAGE;
        }

        // Default to image
        return InstagramMediaType.IMAGE;
    }

    #endregion
}
