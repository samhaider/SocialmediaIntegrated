using Microsoft.Extensions.Options;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SocialMediaAPI.Services;

/// <summary>
/// Implementation of Reddit API operations service
/// Handles post submission, commenting, media upload, and content management
/// </summary>
public class RedditApiService : IRedditApiService
{
    private readonly RedditConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RedditApiService> _logger;

    public RedditApiService(
        IOptions<RedditConfiguration> config,
        IHttpClientFactory httpClientFactory,
        ILogger<RedditApiService> logger)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<RedditSubmitResponse> SubmitPostAsync(string accessToken, RedditPostRequest request)
    {
        try
        {
            var client = CreateOAuthHttpClient(accessToken);

            // Build form data for submission
            var formData = new Dictionary<string, string>
            {
                ["sr"] = request.Subreddit,
                ["title"] = request.Title,
                ["kind"] = request.Kind.ToString().ToLower(),
                ["api_type"] = "json",
                ["sendreplies"] = request.SendReplies.ToString().ToLower(),
                ["nsfw"] = request.Nsfw.ToString().ToLower(),
                ["spoiler"] = request.Spoiler.ToString().ToLower()
            };

            // Add content based on post kind
            switch (request.Kind)
            {
                case RedditPostKind.Self:
                    formData["text"] = request.Text ?? string.Empty;
                    break;
                case RedditPostKind.Link:
                    formData["url"] = request.Url ?? string.Empty;
                    break;
                case RedditPostKind.Image:
                case RedditPostKind.Video:
                    // For image/video posts, url should contain the media asset URL
                    if (!string.IsNullOrEmpty(request.Url))
                    {
                        formData["url"] = request.Url;
                    }
                    break;
            }

            // Add flair if provided
            if (!string.IsNullOrEmpty(request.FlairId))
            {
                formData["flair_id"] = request.FlairId;
            }
            else if (!string.IsNullOrEmpty(request.FlairText))
            {
                formData["flair_text"] = request.FlairText;
            }

            var response = await client.PostAsync($"{_config.ApiBaseUrl}/api/submit",
                new FormUrlEncodedContent(formData));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to submit post. Status: {Status}, Response: {Response}",
                    response.StatusCode, content);
                throw new HttpRequestException($"Post submission failed: {response.StatusCode} - {content}");
            }

            // Parse response
            var responseData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);

            if (responseData == null)
            {
                throw new InvalidOperationException("Failed to parse submission response");
            }

            // Check for errors in response
            if (responseData.ContainsKey("json"))
            {
                var jsonData = responseData["json"];
                if (jsonData.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                {
                    var errorList = JsonSerializer.Deserialize<List<List<string>>>(errors.GetRawText());
                    var errorMessage = errorList?.FirstOrDefault()?.LastOrDefault() ?? "Unknown error";
                    throw new InvalidOperationException($"Reddit API error: {errorMessage}");
                }

                if (jsonData.TryGetProperty("data", out var data))
                {
                    return new RedditSubmitResponse
                    {
                        Success = true,
                        Data = new RedditSubmitData
                        {
                            Id = data.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                            Name = data.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                            Url = data.TryGetProperty("url", out var url) ? url.GetString() ?? string.Empty : string.Empty
                        }
                    };
                }
            }

            throw new InvalidOperationException("Unexpected response format from Reddit API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting post to Reddit");
            throw;
        }
    }

    public async Task<RedditSubmitResponse> SubmitPostWithMediaAsync(string accessToken, RedditPostRequest request, string mediaFilePath)
    {
        try
        {
            // Step 1: Upload media and get asset ID
            var mimeType = GetMimeType(mediaFilePath);
            var mediaResponse = await UploadMediaAssetAsync(accessToken, mediaFilePath, mimeType);

            if (mediaResponse?.Asset == null)
            {
                throw new InvalidOperationException("Failed to upload media asset");
            }

            // Step 2: Submit post with media asset ID
            // The URL should be the media asset websocket URL or we include it in the submission
            var modifiedRequest = new RedditPostRequest
            {
                Subreddit = request.Subreddit,
                Title = request.Title,
                Kind = request.Kind,
                Nsfw = request.Nsfw,
                Spoiler = request.Spoiler,
                SendReplies = request.SendReplies,
                FlairId = request.FlairId,
                FlairText = request.FlairText,
                // Include media asset information
                Url = $"https://reddit.com/media/{mediaResponse.Asset.AssetId}"
            };

            return await SubmitPostAsync(accessToken, modifiedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting post with media to Reddit");
            throw;
        }
    }

    public async Task<RedditCommentResponse> CreateCommentAsync(string accessToken, RedditCommentRequest request)
    {
        try
        {
            var client = CreateOAuthHttpClient(accessToken);

            var formData = new Dictionary<string, string>
            {
                ["thing_id"] = request.ThingId,
                ["text"] = request.Text,
                ["api_type"] = "json"
            };

            var response = await client.PostAsync($"{_config.ApiBaseUrl}/api/comment",
                new FormUrlEncodedContent(formData));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create comment. Status: {Status}, Response: {Response}",
                    response.StatusCode, content);
                throw new HttpRequestException($"Comment creation failed: {response.StatusCode} - {content}");
            }

            // Parse response
            var responseData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);

            if (responseData == null)
            {
                throw new InvalidOperationException("Failed to parse comment response");
            }

            // Check for errors
            if (responseData.ContainsKey("json"))
            {
                var jsonData = responseData["json"];
                if (jsonData.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                {
                    var errorList = JsonSerializer.Deserialize<List<List<string>>>(errors.GetRawText());
                    var errorMessage = errorList?.FirstOrDefault()?.LastOrDefault() ?? "Unknown error";
                    throw new InvalidOperationException($"Reddit API error: {errorMessage}");
                }

                if (jsonData.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("things", out var things) &&
                    things.GetArrayLength() > 0)
                {
                    var thing = things[0];
                    if (thing.TryGetProperty("data", out var thingData) &&
                        thingData.TryGetProperty("id", out var commentId))
                    {
                        return new RedditCommentResponse
                        {
                            Success = true,
                            CommentId = commentId.GetString() ?? string.Empty
                        };
                    }
                }
            }

            return new RedditCommentResponse { Success = true, CommentId = string.Empty };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment on Reddit");
            throw;
        }
    }

    public async Task<bool> EditTextAsync(string accessToken, string thingId, string newText)
    {
        try
        {
            var client = CreateOAuthHttpClient(accessToken);

            var formData = new Dictionary<string, string>
            {
                ["thing_id"] = thingId,
                ["text"] = newText,
                ["api_type"] = "json"
            };

            var response = await client.PostAsync($"{_config.ApiBaseUrl}/api/editusertext",
                new FormUrlEncodedContent(formData));

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing text on Reddit");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string accessToken, string thingId)
    {
        try
        {
            var client = CreateOAuthHttpClient(accessToken);

            var formData = new Dictionary<string, string>
            {
                ["id"] = thingId
            };

            var response = await client.PostAsync($"{_config.ApiBaseUrl}/api/del",
                new FormUrlEncodedContent(formData));

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting content on Reddit");
            return false;
        }
    }

    public async Task<RedditMediaAssetResponse> UploadMediaAssetAsync(string accessToken, string filePath, string mimeType)
    {
        try
        {
            var client = CreateOAuthHttpClient(accessToken);

            // Step 1: Request upload lease
            var formData = new Dictionary<string, string>
            {
                ["filepath"] = Path.GetFileName(filePath),
                ["mimetype"] = mimeType
            };

            var response = await client.PostAsync($"{_config.ApiBaseUrl}/api/media/asset.json",
                new FormUrlEncodedContent(formData));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get media upload lease. Status: {Status}, Response: {Response}",
                    response.StatusCode, content);
                throw new HttpRequestException($"Media upload lease failed: {response.StatusCode}");
            }

            var assetData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);

            if (assetData == null || !assetData.ContainsKey("args"))
            {
                throw new InvalidOperationException("Failed to parse media asset response");
            }

            var args = assetData["args"];
            var uploadUrl = args.GetProperty("action").GetString();
            var assetId = args.GetProperty("fields").EnumerateArray()
                .FirstOrDefault(f => f.GetProperty("name").GetString() == "key")
                .GetProperty("value").GetString();

            // Step 2: Upload file to S3 (or designated upload URL)
            if (!string.IsNullOrEmpty(uploadUrl))
            {
                await UploadFileToUrl(uploadUrl, filePath, args);
            }

            return new RedditMediaAssetResponse
            {
                Asset = new RedditMediaAssetData
                {
                    AssetId = assetId ?? string.Empty,
                    ProcessingState = "uploaded"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media asset to Reddit");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetSubmitRequirementsAsync(string accessToken, string subreddit)
    {
        try
        {
            var client = CreateOAuthHttpClient(accessToken);

            var response = await client.GetAsync($"{_config.ApiBaseUrl}/api/v1/{subreddit}/post_requirements");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get submit requirements for r/{Subreddit}. Status: {Status}",
                    subreddit, response.StatusCode);
                return new Dictionary<string, object>();
            }

            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            return data ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submit requirements for r/{Subreddit}", subreddit);
            return new Dictionary<string, object>();
        }
    }

    private async Task UploadFileToUrl(string uploadUrl, string filePath, JsonElement fields)
    {
        using var fileStream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();

        // Add all fields from the upload lease
        foreach (var field in fields.GetProperty("fields").EnumerateArray())
        {
            var name = field.GetProperty("name").GetString();
            var value = field.GetProperty("value").GetString();
            if (name != null && value != null)
            {
                content.Add(new StringContent(value), name);
            }
        }

        // Add file
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(uploadUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"File upload to {uploadUrl} failed: {response.StatusCode}");
        }
    }

    private string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }

    private HttpClient CreateOAuthHttpClient(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_config.UserAgent);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
