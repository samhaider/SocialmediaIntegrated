using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;
using SocialMediaAPI.DTOs;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;

namespace SocialMediaAPI.Services.Platforms;

/// <summary>
/// Facebook Platform implementation for Facebook Page posting and management
/// Implements the Facebook Graph API v19.0 for posting to company/business pages
/// </summary>
public class FacebookPlatform : ISocialMediaPlatform
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FacebookPlatform> _logger;
    private const string API_VERSION = "v19.0";
    private string GraphApiBaseUrl => _configuration["Facebook:GraphApiBaseUrl"] ?? "https://graph.facebook.com";

    public FacebookPlatform(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FacebookPlatform> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public string PlatformName => "Facebook";

    /// <summary>
    /// Validates the Facebook Page access token by calling the Graph API
    /// </summary>
    /// <param name="accessToken">Page access token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
    public async Task<bool> ValidateConnectionAsync(string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }

            var httpClient = _httpClientFactory.CreateClient();

            // Validate token by calling /me endpoint
            var url = $"{GraphApiBaseUrl}/{API_VERSION}/me?access_token={accessToken}";
            var response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Facebook token validation failed: {Error}", errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Facebook connection");
            return false;
        }
    }

    /// <summary>
    /// Publishes a post to a Facebook Page
    /// </summary>
    /// <param name="accessToken">Page access token (not user token)</param>
    /// <param name="content">Post message/content</param>
    /// <param name="mediaUrls">Optional list of media URLs to include</param>
    /// <returns>Facebook post ID (format: {page-id}_{post-id})</returns>
    public async Task<string> PublishPostAsync(string accessToken, string content, List<string>? mediaUrls)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException("Access token is required");
            }

            var httpClient = _httpClientFactory.CreateClient();

            // First, get the page ID from the access token
            var pageId = await GetPageIdFromTokenAsync(accessToken);
            if (string.IsNullOrEmpty(pageId))
            {
                throw new Exception("Failed to retrieve page ID from access token");
            }

            // If media URLs are provided, handle photo/video posts separately
            if (mediaUrls != null && mediaUrls.Any())
            {
                return await PublishMediaPostAsync(httpClient, pageId, accessToken, content, mediaUrls);
            }

            // Standard text post to page feed
            var url = $"{GraphApiBaseUrl}/{API_VERSION}/{pageId}/feed";
            var postData = new Dictionary<string, string>
            {
                { "message", content },
                { "access_token", accessToken }
            };

            var formContent = new FormUrlEncodedContent(postData);
            var response = await httpClient.PostAsync(url, formContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<FacebookErrorResponse>(responseContent);
                var errorMessage = error?.Error?.Message ?? "Unknown error";
                _logger.LogError("Facebook post failed: {Error}", errorMessage);
                throw new Exception($"Failed to publish post: {errorMessage}");
            }

            var postResponse = JsonSerializer.Deserialize<FacebookPostResponse>(responseContent);
            return postResponse?.Id ?? throw new Exception("No post ID returned from Facebook");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing post to Facebook");
            throw;
        }
    }

    /// <summary>
    /// Retrieves post analytics/insights from Facebook
    /// </summary>
    /// <param name="accessToken">Page access token</param>
    /// <param name="platformPostId">Facebook post ID</param>
    /// <returns>Post analytics including likes, comments, shares, views, and clicks</returns>
    public async Task<PostAnalytics> GetPostAnalyticsAsync(string accessToken, string platformPostId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            // Get post engagement metrics
            var url = $"{GraphApiBaseUrl}/{API_VERSION}/{platformPostId}?" +
                     $"fields=likes.summary(true),comments.summary(true),shares&" +
                     $"access_token={accessToken}";

            var response = await httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Facebook analytics: {Content}", content);
                // Return empty analytics if fetch fails
                return new PostAnalytics
                {
                    Platform = PlatformName,
                    FetchedAt = DateTime.UtcNow
                };
            }

            var data = JsonSerializer.Deserialize<JsonElement>(content);

            var analytics = new PostAnalytics
            {
                Platform = PlatformName,
                Likes = data.TryGetProperty("likes", out var likes) &&
                       likes.TryGetProperty("summary", out var likesSummary) &&
                       likesSummary.TryGetProperty("total_count", out var likesCount)
                       ? likesCount.GetInt32() : 0,

                Comments = data.TryGetProperty("comments", out var comments) &&
                          comments.TryGetProperty("summary", out var commentsSummary) &&
                          commentsSummary.TryGetProperty("total_count", out var commentsCount)
                          ? commentsCount.GetInt32() : 0,

                Shares = data.TryGetProperty("shares", out var shares) &&
                        shares.TryGetProperty("count", out var sharesCount)
                        ? sharesCount.GetInt32() : 0,

                // Note: Views and clicks require page insights API with additional permissions
                Views = 0,
                Clicks = 0,
                FetchedAt = DateTime.UtcNow
            };

            // Try to get impressions from insights (requires additional permissions)
            try
            {
                var insightsUrl = $"{GraphApiBaseUrl}/{API_VERSION}/{platformPostId}/insights?" +
                                 $"metric=post_impressions,post_engaged_users&" +
                                 $"access_token={accessToken}";

                var insightsResponse = await httpClient.GetAsync(insightsUrl);
                if (insightsResponse.IsSuccessStatusCode)
                {
                    var insightsContent = await insightsResponse.Content.ReadAsStringAsync();
                    var insightsData = JsonSerializer.Deserialize<JsonElement>(insightsContent);

                    if (insightsData.TryGetProperty("data", out var dataArray))
                    {
                        foreach (var insight in dataArray.EnumerateArray())
                        {
                            if (insight.TryGetProperty("name", out var name))
                            {
                                var metricName = name.GetString();
                                if (insight.TryGetProperty("values", out var values) && values.GetArrayLength() > 0)
                                {
                                    var firstValue = values[0];
                                    if (firstValue.TryGetProperty("value", out var value))
                                    {
                                        if (metricName == "post_impressions")
                                        {
                                            analytics.Views = value.GetInt32();
                                        }
                                        else if (metricName == "post_engaged_users")
                                        {
                                            analytics.Clicks = value.GetInt32();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch Facebook post insights (may require additional permissions)");
            }

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching post analytics from Facebook");
            throw;
        }
    }

    /// <summary>
    /// Retrieves comments on a Facebook post
    /// </summary>
    /// <param name="accessToken">Page access token</param>
    /// <param name="platformPostId">Facebook post ID</param>
    /// <returns>List of comments</returns>
    public async Task<List<Comment>> GetCommentsAsync(string accessToken, string platformPostId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{GraphApiBaseUrl}/{API_VERSION}/{platformPostId}/comments?" +
                     $"fields=id,from,message,created_time&" +
                     $"access_token={accessToken}";

            var response = await httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch comments: {Content}", content);
                return new List<Comment>();
            }

            var data = JsonSerializer.Deserialize<JsonElement>(content);
            var comments = new List<Comment>();

            if (data.TryGetProperty("data", out var dataArray))
            {
                foreach (var commentData in dataArray.EnumerateArray())
                {
                    var comment = new Comment
                    {
                        Platform = PlatformName,
                        PlatformCommentId = commentData.GetProperty("id").GetString() ?? string.Empty,
                        Content = commentData.TryGetProperty("message", out var message)
                                 ? message.GetString() ?? string.Empty
                                 : string.Empty,
                        AuthorName = commentData.TryGetProperty("from", out var from) &&
                                    from.TryGetProperty("name", out var name)
                                    ? name.GetString()
                                    : null,
                        AuthorId = commentData.TryGetProperty("from", out var fromId) &&
                                  fromId.TryGetProperty("id", out var id)
                                  ? id.GetString()
                                  : null,
                        CreatedAt = commentData.TryGetProperty("created_time", out var createdTime)
                                   ? DateTime.Parse(createdTime.GetString() ?? DateTime.UtcNow.ToString())
                                   : DateTime.UtcNow,
                        IsReplied = false
                    };
                    comments.Add(comment);
                }
            }

            return comments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching comments from Facebook");
            throw;
        }
    }

    /// <summary>
    /// Replies to a comment on Facebook
    /// </summary>
    /// <param name="accessToken">Page access token</param>
    /// <param name="commentId">Facebook comment ID to reply to</param>
    /// <param name="reply">Reply message</param>
    /// <returns>True if reply was successful</returns>
    public async Task<bool> ReplyToCommentAsync(string accessToken, string commentId, string reply)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{GraphApiBaseUrl}/{API_VERSION}/{commentId}/comments";

            var postData = new Dictionary<string, string>
            {
                { "message", reply },
                { "access_token", accessToken }
            };

            var formContent = new FormUrlEncodedContent(postData);
            var response = await httpClient.PostAsync(url, formContent);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to reply to comment: {Content}", content);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replying to Facebook comment");
            return false;
        }
    }

    /// <summary>
    /// Retrieves direct messages (conversations) for a Facebook Page
    /// Note: Requires additional permissions (pages_messaging)
    /// </summary>
    /// <param name="accessToken">Page access token</param>
    /// <returns>List of direct messages</returns>
    public async Task<List<DirectMessage>> GetDirectMessagesAsync(string accessToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            // Get page ID first
            var pageId = await GetPageIdFromTokenAsync(accessToken);
            if (string.IsNullOrEmpty(pageId))
            {
                return new List<DirectMessage>();
            }

            // Get conversations
            var url = $"{GraphApiBaseUrl}/{API_VERSION}/{pageId}/conversations?" +
                     $"fields=messages{{message,from,created_time}}&" +
                     $"access_token={accessToken}";

            var response = await httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch direct messages (may require pages_messaging permission): {Content}", content);
                return new List<DirectMessage>();
            }

            var messages = new List<DirectMessage>();
            var data = JsonSerializer.Deserialize<JsonElement>(content);

            if (data.TryGetProperty("data", out var conversations))
            {
                foreach (var conversation in conversations.EnumerateArray())
                {
                    if (conversation.TryGetProperty("messages", out var messagesData) &&
                        messagesData.TryGetProperty("data", out var messageArray))
                    {
                        foreach (var msg in messageArray.EnumerateArray())
                        {
                            var message = new DirectMessage
                            {
                                Platform = PlatformName,
                                PlatformMessageId = msg.GetProperty("id").GetString() ?? string.Empty,
                                Content = msg.TryGetProperty("message", out var msgText)
                                         ? msgText.GetString() ?? string.Empty
                                         : string.Empty,
                                SenderName = msg.TryGetProperty("from", out var from) &&
                                            from.TryGetProperty("name", out var name)
                                            ? name.GetString()
                                            : null,
                                SenderId = msg.TryGetProperty("from", out var fromId) &&
                                          fromId.TryGetProperty("id", out var id)
                                          ? id.GetString()
                                          : null,
                                IsIncoming = true, // Will need logic to determine direction
                                CreatedAt = msg.TryGetProperty("created_time", out var createdTime)
                                           ? DateTime.Parse(createdTime.GetString() ?? DateTime.UtcNow.ToString())
                                           : DateTime.UtcNow,
                                IsRead = false
                            };
                            messages.Add(message);
                        }
                    }
                }
            }

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching direct messages from Facebook");
            return new List<DirectMessage>();
        }
    }

    #region Helper Methods

    /// <summary>
    /// Gets the Page ID from a page access token
    /// </summary>
    private async Task<string?> GetPageIdFromTokenAsync(string accessToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{GraphApiBaseUrl}/{API_VERSION}/me?access_token={accessToken}";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);

            return data.TryGetProperty("id", out var id) ? id.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving page ID from token");
            return null;
        }
    }

    /// <summary>
    /// Publishes a post with media (photos/videos) to Facebook Page
    /// </summary>
    private async Task<string> PublishMediaPostAsync(
        HttpClient httpClient,
        string pageId,
        string accessToken,
        string content,
        List<string> mediaUrls)
    {
        try
        {
            // For simplicity, post the first media URL
            // In production, you might want to handle multiple media items differently
            var mediaUrl = mediaUrls.First();
            var isVideo = mediaUrl.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                         mediaUrl.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                         mediaUrl.EndsWith(".avi", StringComparison.OrdinalIgnoreCase);

            string endpoint = isVideo ? "videos" : "photos";
            var url = $"{GraphApiBaseUrl}/{API_VERSION}/{pageId}/{endpoint}";

            var postData = new Dictionary<string, string>
            {
                { "url", mediaUrl },
                { "access_token", accessToken }
            };

            if (!string.IsNullOrEmpty(content))
            {
                postData.Add(isVideo ? "description" : "caption", content);
            }

            var formContent = new FormUrlEncodedContent(postData);
            var response = await httpClient.PostAsync(url, formContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<FacebookErrorResponse>(responseContent);
                throw new Exception($"Failed to publish media post: {error?.Error?.Message}");
            }

            var postResponse = JsonSerializer.Deserialize<FacebookPostResponse>(responseContent);
            return postResponse?.Id ?? throw new Exception("No post ID returned from Facebook");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing media post to Facebook");
            throw;
        }
    }

    #endregion
}
