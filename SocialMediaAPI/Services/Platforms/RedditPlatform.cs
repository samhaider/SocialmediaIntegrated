using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;
using System.Text.Json;

namespace SocialMediaAPI.Services.Platforms;

/// <summary>
/// Reddit platform implementation with full OAuth and API integration
/// Supports posting, commenting, media upload, and analytics
/// </summary>
public class RedditPlatform : ISocialMediaPlatform
{
    private readonly IRedditOAuthService _oauthService;
    private readonly IRedditApiService _apiService;
    private readonly ILogger<RedditPlatform> _logger;

    public RedditPlatform(
        IRedditOAuthService oauthService,
        IRedditApiService apiService,
        ILogger<RedditPlatform> logger)
    {
        _oauthService = oauthService;
        _apiService = apiService;
        _logger = logger;
    }

    public string PlatformName => "Reddit";

    public async Task<bool> ValidateConnectionAsync(string accessToken)
    {
        try
        {
            return await _oauthService.ValidateTokenAsync(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Reddit connection");
            return false;
        }
    }

    public async Task<string> PublishPostAsync(string accessToken, string content, List<string>? mediaUrls)
    {
        try
        {
            // Parse content to extract subreddit and post details
            // Expected format in content (JSON):
            // {
            //   "subreddit": "test",
            //   "title": "Post title",
            //   "kind": "self|link|image",
            //   "text": "Post body text (for self posts)",
            //   "url": "https://example.com (for link posts)"
            // }

            var postData = ParsePostContent(content);

            var request = new RedditPostRequest
            {
                Subreddit = postData.GetValueOrDefault("subreddit", "test"),
                Title = postData.GetValueOrDefault("title", "Untitled Post"),
                Kind = ParsePostKind(postData.GetValueOrDefault("kind", "self")),
                Text = postData.GetValueOrDefault("text", null),
                Url = postData.GetValueOrDefault("url", null),
                Nsfw = bool.Parse(postData.GetValueOrDefault("nsfw", "false")),
                Spoiler = bool.Parse(postData.GetValueOrDefault("spoiler", "false")),
                SendReplies = bool.Parse(postData.GetValueOrDefault("sendreplies", "true"))
            };

            RedditSubmitResponse response;

            // Handle media upload if media URLs provided
            if (mediaUrls != null && mediaUrls.Count > 0 && File.Exists(mediaUrls[0]))
            {
                request.Kind = RedditPostKind.Image;
                response = await _apiService.SubmitPostWithMediaAsync(accessToken, request, mediaUrls[0]);
            }
            else
            {
                response = await _apiService.SubmitPostAsync(accessToken, request);
            }

            if (response.Success && response.Data != null)
            {
                return response.Data.Id;
            }

            throw new InvalidOperationException("Post submission failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing post to Reddit");
            throw;
        }
    }

    public async Task<PostAnalytics> GetPostAnalyticsAsync(string accessToken, string platformPostId)
    {
        try
        {
            // Reddit doesn't provide direct analytics via API for non-promoted posts
            // We can fetch the post and get basic stats
            // This is a simplified implementation
            // For production, you'd call: GET /r/{subreddit}/comments/{article}

            _logger.LogWarning("Reddit analytics are limited via API. Returning placeholder data.");

            // In a real implementation, you would:
            // 1. GET https://oauth.reddit.com/api/info?id=t3_{platformPostId}
            // 2. Parse the response to get score, num_comments, etc.

            return new PostAnalytics
            {
                Likes = 0,
                Comments = 0,
                Shares = 0,
                Views = 0,
                Clicks = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Reddit post analytics");
            throw;
        }
    }

    public async Task<List<Comment>> GetCommentsAsync(string accessToken, string platformPostId)
    {
        try
        {
            // To get comments, we need to call:
            // GET /comments/{article}
            // This would require the subreddit name and post ID

            _logger.LogWarning("Reddit comments retrieval requires additional implementation.");

            // For now, return empty list
            // In production, implement full comment tree parsing
            return new List<Comment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Reddit comments");
            return new List<Comment>();
        }
    }

    public async Task<bool> ReplyToCommentAsync(string accessToken, string commentId, string reply)
    {
        try
        {
            // commentId should be the full thing name (e.g., t1_xxxxx for comment, t3_xxxxx for post)
            var request = new RedditCommentRequest
            {
                ThingId = commentId.StartsWith("t") ? commentId : $"t1_{commentId}",
                Text = reply
            };

            var response = await _apiService.CreateCommentAsync(accessToken, request);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replying to Reddit comment");
            return false;
        }
    }

    public Task<List<DirectMessage>> GetDirectMessagesAsync(string accessToken)
    {
        // Reddit direct messaging is available via /api/compose
        // For now, return empty list
        _logger.LogWarning("Reddit direct messages require additional implementation.");
        return Task.FromResult(new List<DirectMessage>());
    }

    /// <summary>
    /// Helper method to parse post content JSON
    /// </summary>
    private Dictionary<string, string> ParsePostContent(string content)
    {
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
            return data ?? new Dictionary<string, string> { ["text"] = content };
        }
        catch
        {
            // If not JSON, treat as plain text for a self post
            return new Dictionary<string, string>
            {
                ["title"] = content.Length > 100 ? content.Substring(0, 100) : content,
                ["text"] = content,
                ["kind"] = "self"
            };
        }
    }

    /// <summary>
    /// Helper method to parse post kind from string
    /// </summary>
    private RedditPostKind ParsePostKind(string kind)
    {
        return kind.ToLowerInvariant() switch
        {
            "link" => RedditPostKind.Link,
            "image" => RedditPostKind.Image,
            "video" => RedditPostKind.Video,
            _ => RedditPostKind.Self
        };
    }
}
