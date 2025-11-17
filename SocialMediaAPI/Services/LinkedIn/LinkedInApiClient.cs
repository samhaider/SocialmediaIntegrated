using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SocialMediaAPI.Configuration;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Services.LinkedIn;

public interface ILinkedInApiClient
{
    Task<LinkedInCreatePostResponse> CreatePostAsync(string accessToken, LinkedInCreatePostRequest request);
    Task<LinkedInSocialMetadata?> GetPostAnalyticsAsync(string accessToken, string postUrn);
    Task<List<LinkedInComment>> GetPostCommentsAsync(string accessToken, string postUrn);
    Task<string> ReplyToCommentAsync(string accessToken, string parentCommentUrn, string text, string actorUrn);
}

public class LinkedInApiClient : ILinkedInApiClient
{
    private readonly HttpClient _httpClient;
    private readonly LinkedInSettings _settings;
    private readonly ILogger<LinkedInApiClient> _logger;

    public LinkedInApiClient(
        HttpClient httpClient,
        IOptions<LinkedInSettings> settings,
        ILogger<LinkedInApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<LinkedInCreatePostResponse> CreatePostAsync(
        string accessToken,
        LinkedInCreatePostRequest request)
    {
        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_settings.ApiBaseUrl}/posts");

            httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
            httpRequest.Headers.Add("LinkedIn-Version", _settings.ApiVersion);
            httpRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Creating LinkedIn post for author: {Author}", request.Author);
            _logger.LogDebug("Post request payload: {Payload}", jsonContent);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create post. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);

                // Try to parse error response
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<LinkedInErrorResponse>(responseContent);
                    throw new HttpRequestException(
                        $"LinkedIn API error: {errorResponse?.Message ?? response.StatusCode.ToString()}");
                }
                catch (JsonException)
                {
                    throw new HttpRequestException($"Post creation failed: {response.StatusCode} - {responseContent}");
                }
            }

            // LinkedIn returns the post URN in the X-RestLi-Id header
            var postUrn = response.Headers.TryGetValues("X-RestLi-Id", out var values)
                ? values.FirstOrDefault()
                : null;

            if (string.IsNullOrEmpty(postUrn))
            {
                // Try to extract from response body
                var postResponse = JsonSerializer.Deserialize<LinkedInCreatePostResponse>(responseContent, jsonOptions);
                postUrn = postResponse?.Id ?? postResponse?.Urn;
            }

            if (string.IsNullOrEmpty(postUrn))
            {
                _logger.LogWarning("Post created but URN not found in response. Response: {Response}", responseContent);
                postUrn = $"urn:li:share:{Guid.NewGuid()}"; // Fallback
            }

            _logger.LogInformation("Successfully created LinkedIn post with URN: {PostUrn}", postUrn);

            return new LinkedInCreatePostResponse
            {
                Id = postUrn,
                Urn = postUrn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating LinkedIn post");
            throw;
        }
    }

    public async Task<LinkedInSocialMetadata?> GetPostAnalyticsAsync(string accessToken, string postUrn)
    {
        try
        {
            // Encode the URN for the URL
            var encodedUrn = Uri.EscapeDataString(postUrn);

            var httpRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_settings.ApiBaseUrl}/socialMetadata/{encodedUrn}");

            httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
            httpRequest.Headers.Add("LinkedIn-Version", _settings.ApiVersion);
            httpRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get post analytics. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Post not found: {PostUrn}", postUrn);
                    return null;
                }

                throw new HttpRequestException($"Get analytics failed: {response.StatusCode}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var metadata = JsonSerializer.Deserialize<LinkedInSocialMetadata>(responseContent, options);

            _logger.LogInformation("Successfully retrieved analytics for post: {PostUrn}", postUrn);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post analytics for URN: {PostUrn}", postUrn);
            throw;
        }
    }

    public async Task<List<LinkedInComment>> GetPostCommentsAsync(string accessToken, string postUrn)
    {
        try
        {
            var encodedUrn = Uri.EscapeDataString(postUrn);

            var httpRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_settings.ApiBaseUrl}/socialActions/{encodedUrn}/comments");

            httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
            httpRequest.Headers.Add("LinkedIn-Version", _settings.ApiVersion);
            httpRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get post comments. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Post not found: {PostUrn}", postUrn);
                    return new List<LinkedInComment>();
                }

                throw new HttpRequestException($"Get comments failed: {response.StatusCode}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var commentsResponse = JsonSerializer.Deserialize<LinkedInCommentsResponse>(responseContent, options);

            _logger.LogInformation("Successfully retrieved {Count} comments for post: {PostUrn}",
                commentsResponse?.Elements?.Count ?? 0, postUrn);

            return commentsResponse?.Elements ?? new List<LinkedInComment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post comments for URN: {PostUrn}", postUrn);
            throw;
        }
    }

    public async Task<string> ReplyToCommentAsync(
        string accessToken,
        string parentCommentUrn,
        string text,
        string actorUrn)
    {
        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{_settings.ApiBaseUrl}/socialActions/{Uri.EscapeDataString(parentCommentUrn)}/comments");

            httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
            httpRequest.Headers.Add("LinkedIn-Version", _settings.ApiVersion);
            httpRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

            var requestBody = new
            {
                actor = actorUrn,
                message = new { text }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to reply to comment. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"Reply to comment failed: {response.StatusCode}");
            }

            // Get comment URN from response header
            var commentUrn = response.Headers.TryGetValues("X-RestLi-Id", out var values)
                ? values.FirstOrDefault()
                : $"urn:li:comment:{Guid.NewGuid()}";

            _logger.LogInformation("Successfully replied to comment. Comment URN: {CommentUrn}", commentUrn);
            return commentUrn ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replying to comment: {ParentCommentUrn}", parentCommentUrn);
            throw;
        }
    }
}
