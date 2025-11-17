using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SocialMediaAPI.Data;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;
using SocialMediaAPI.Services.LinkedIn;

namespace SocialMediaAPI.Services.Platforms;

public class LinkedInPlatform : ISocialMediaPlatform
{
    private readonly ILinkedInOAuthService _oauthService;
    private readonly ILinkedInApiClient _apiClient;
    private readonly ILinkedInMediaService _mediaService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LinkedInPlatform> _logger;

    public string PlatformName => "LinkedIn";

    public LinkedInPlatform(
        ILinkedInOAuthService oauthService,
        ILinkedInApiClient apiClient,
        ILinkedInMediaService mediaService,
        ApplicationDbContext context,
        ILogger<LinkedInPlatform> logger)
    {
        _oauthService = oauthService;
        _apiClient = apiClient;
        _mediaService = mediaService;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ValidateConnectionAsync(string accessToken)
    {
        try
        {
            return await _oauthService.ValidateTokenAsync(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating LinkedIn connection");
            return false;
        }
    }

    public async Task<string> PublishPostAsync(string accessToken, string content, List<string>? mediaUrls)
    {
        try
        {
            _logger.LogInformation("Publishing LinkedIn post with content length: {Length}", content?.Length ?? 0);

            // Get the social account to determine author URN
            var account = await _context.SocialAccounts
                .FirstOrDefaultAsync(sa => sa.AccessToken == accessToken && sa.Platform == PlatformName);

            if (account == null)
            {
                _logger.LogError("Social account not found for provided access token");
                throw new InvalidOperationException("Social account not found");
            }

            // Determine the author URN (organization or person)
            string authorUrn;
            if (account.AccountType == "Organization" && !string.IsNullOrEmpty(account.OrganizationId))
            {
                authorUrn = account.OrganizationId; // Already in URN format
                _logger.LogInformation("Posting as organization: {OrganizationUrn}", authorUrn);
            }
            else
            {
                // Use person URN from account ID
                authorUrn = account.AccountId.StartsWith("urn:li:")
                    ? account.AccountId
                    : $"urn:li:person:{account.AccountId}";
                _logger.LogInformation("Posting as person: {PersonUrn}", authorUrn);
            }

            // Create the post request
            var postRequest = new LinkedInCreatePostRequest
            {
                Author = authorUrn,
                Commentary = content ?? string.Empty,
                Visibility = "PUBLIC",
                LifecycleState = "PUBLISHED",
                Distribution = new LinkedInDistribution
                {
                    FeedDistribution = "MAIN_FEED",
                    TargetEntities = new List<string>(),
                    ThirdPartyDistributionChannels = new List<string>()
                }
            };

            // Handle media uploads if provided
            if (mediaUrls?.Any() == true)
            {
                _logger.LogInformation("Uploading {Count} media files", mediaUrls.Count);

                var mediaAssetUrns = new List<string>();

                foreach (var mediaUrl in mediaUrls)
                {
                    try
                    {
                        // Download the media from URL
                        var mediaData = await DownloadMediaFromUrlAsync(mediaUrl);
                        var fileName = Path.GetFileName(new Uri(mediaUrl).LocalPath);

                        // Determine if it's an image or video based on extension
                        var extension = Path.GetExtension(fileName).ToLowerInvariant();
                        string assetUrn;

                        if (IsImageFile(extension))
                        {
                            assetUrn = await _mediaService.UploadImageAsync(
                                accessToken, authorUrn, mediaData, fileName);
                        }
                        else if (IsVideoFile(extension))
                        {
                            assetUrn = await _mediaService.UploadVideoAsync(
                                accessToken, authorUrn, mediaData, fileName);
                        }
                        else
                        {
                            _logger.LogWarning("Unsupported media type: {Extension}", extension);
                            continue;
                        }

                        mediaAssetUrns.Add(assetUrn);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload media: {MediaUrl}", mediaUrl);
                        // Continue with other media files
                    }
                }

                // Add media to post (LinkedIn supports single media or article for now)
                if (mediaAssetUrns.Any())
                {
                    postRequest.Content = new LinkedInPostContent
                    {
                        Media = new LinkedInMediaContent
                        {
                            Id = mediaAssetUrns.First(), // Use first media asset
                            Title = "Shared media"
                        }
                    };
                }
            }

            // Create the post
            var postResponse = await _apiClient.CreatePostAsync(accessToken, postRequest);

            _logger.LogInformation("Successfully published LinkedIn post: {PostId}", postResponse.Id);
            return postResponse.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing LinkedIn post");
            throw;
        }
    }

    public async Task<PostAnalytics> GetPostAnalyticsAsync(string accessToken, string platformPostId)
    {
        try
        {
            _logger.LogInformation("Fetching analytics for LinkedIn post: {PostId}", platformPostId);

            var metadata = await _apiClient.GetPostAnalyticsAsync(accessToken, platformPostId);

            if (metadata?.TotalShareStatistics == null)
            {
                _logger.LogWarning("No analytics data available for post: {PostId}", platformPostId);
                return new PostAnalytics
                {
                    Likes = 0,
                    Comments = 0,
                    Shares = 0,
                    Views = 0,
                    Clicks = 0
                };
            }

            var stats = metadata.TotalShareStatistics;

            return new PostAnalytics
            {
                Likes = stats.LikeCount,
                Comments = stats.CommentCount,
                Shares = stats.ShareCount,
                Views = stats.ImpressionCount,
                Clicks = stats.ClickCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LinkedIn post analytics");
            throw;
        }
    }

    public async Task<List<Comment>> GetCommentsAsync(string accessToken, string platformPostId)
    {
        try
        {
            _logger.LogInformation("Fetching comments for LinkedIn post: {PostId}", platformPostId);

            var linkedInComments = await _apiClient.GetPostCommentsAsync(accessToken, platformPostId);

            return linkedInComments.Select(c => new Comment
            {
                Platform = PlatformName,
                PlatformCommentId = c.Id,
                AuthorName = c.Actor ?? "Unknown",
                Content = c.Message?.Text ?? string.Empty,
                CreatedAt = c.Created?.Time != null
                    ? DateTimeOffset.FromUnixTimeMilliseconds(c.Created.Time).DateTime
                    : DateTime.UtcNow
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LinkedIn comments");
            throw;
        }
    }

    public async Task<bool> ReplyToCommentAsync(string accessToken, string commentId, string reply)
    {
        try
        {
            _logger.LogInformation("Replying to LinkedIn comment: {CommentId}", commentId);

            // Get the social account to determine actor URN
            var account = await _context.SocialAccounts
                .FirstOrDefaultAsync(sa => sa.AccessToken == accessToken && sa.Platform == PlatformName);

            if (account == null)
            {
                _logger.LogError("Social account not found for provided access token");
                return false;
            }

            // Determine the actor URN (organization or person)
            string actorUrn;
            if (account.AccountType == "Organization" && !string.IsNullOrEmpty(account.OrganizationId))
            {
                actorUrn = account.OrganizationId;
            }
            else
            {
                actorUrn = account.AccountId.StartsWith("urn:li:")
                    ? account.AccountId
                    : $"urn:li:person:{account.AccountId}";
            }

            var replyUrn = await _apiClient.ReplyToCommentAsync(
                accessToken, commentId, reply, actorUrn);

            _logger.LogInformation("Successfully replied to comment. Reply URN: {ReplyUrn}", replyUrn);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replying to LinkedIn comment");
            return false;
        }
    }

    public Task<List<DirectMessage>> GetDirectMessagesAsync(string accessToken)
    {
        // LinkedIn Direct Messages API is not implemented in this version
        // This would require separate implementation using LinkedIn's Messaging API
        _logger.LogWarning("LinkedIn Direct Messages API not implemented");
        return Task.FromResult(new List<DirectMessage>());
    }

    // Helper methods

    private async Task<byte[]> DownloadMediaFromUrlAsync(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            return await httpClient.GetByteArrayAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading media from URL: {Url}", url);
            throw;
        }
    }

    private static bool IsImageFile(string extension)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        return imageExtensions.Contains(extension);
    }

    private static bool IsVideoFile(string extension)
    {
        var videoExtensions = new[] { ".mp4", ".mov", ".avi", ".wmv", ".flv", ".webm" };
        return videoExtensions.Contains(extension);
    }
}
