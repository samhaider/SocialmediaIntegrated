using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SocialMediaAPI.Configuration;
using SocialMediaAPI.DTOs.Instagram;

namespace SocialMediaAPI.Services.Instagram;

/// <summary>
/// Service for interacting with Instagram Graph API
/// </summary>
public interface IInstagramGraphApiService
{
    Task<InstagramOAuthResponse> ExchangeCodeForTokenAsync(string code);
    Task<LongLivedTokenResponse> GetLongLivedTokenAsync(string shortLivedToken);
    Task<LongLivedTokenResponse> RefreshLongLivedTokenAsync(string currentToken);
    Task<TokenDebugData> DebugTokenAsync(string accessToken);
    Task<List<FacebookPage>> GetUserPagesAsync(string userAccessToken);
    Task<InstagramBusinessAccount?> GetInstagramBusinessAccountAsync(string pageId, string pageAccessToken);
    Task<string> CreateMediaContainerAsync(string igUserId, string accessToken, CreateMediaContainerRequest request);
    Task<string> PublishMediaContainerAsync(string igUserId, string accessToken, string containerId);
    Task<ContainerStatusResponse> GetContainerStatusAsync(string containerId, string accessToken);
    Task<InstagramMediaInsights> GetMediaInsightsAsync(string mediaId, string accessToken, string[] metrics);
}

public class InstagramGraphApiService : IInstagramGraphApiService
{
    private readonly HttpClient _httpClient;
    private readonly InstagramSettings _settings;
    private readonly ILogger<InstagramGraphApiService> _logger;

    public InstagramGraphApiService(
        HttpClient httpClient,
        IOptions<InstagramSettings> settings,
        ILogger<InstagramGraphApiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Exchange OAuth code for short-lived access token
    /// </summary>
    public async Task<InstagramOAuthResponse> ExchangeCodeForTokenAsync(string code)
    {
        try
        {
            var url = $"{_settings.GraphApiBaseUrl}/oauth/access_token" +
                     $"?client_id={_settings.AppId}" +
                     $"&client_secret={_settings.AppSecret}" +
                     $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}" +
                     $"&code={code}";

            var response = await ExecuteWithRetryAsync<InstagramOAuthResponse>(
                () => _httpClient.GetAsync(url));

            _logger.LogInformation("Successfully exchanged code for access token");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange code for token");
            throw;
        }
    }

    /// <summary>
    /// Convert short-lived token to long-lived token (60 days)
    /// </summary>
    public async Task<LongLivedTokenResponse> GetLongLivedTokenAsync(string shortLivedToken)
    {
        try
        {
            var url = $"{_settings.GraphApiBaseUrl}/oauth/access_token" +
                     $"?grant_type=fb_exchange_token" +
                     $"&client_id={_settings.AppId}" +
                     $"&client_secret={_settings.AppSecret}" +
                     $"&fb_exchange_token={shortLivedToken}";

            var response = await ExecuteWithRetryAsync<LongLivedTokenResponse>(
                () => _httpClient.GetAsync(url));

            _logger.LogInformation("Successfully obtained long-lived token");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get long-lived token");
            throw;
        }
    }

    /// <summary>
    /// Refresh a long-lived token (extends for another 60 days)
    /// </summary>
    public async Task<LongLivedTokenResponse> RefreshLongLivedTokenAsync(string currentToken)
    {
        try
        {
            var url = $"{_settings.GraphApiBaseUrl}/oauth/access_token" +
                     $"?grant_type=fb_exchange_token" +
                     $"&client_id={_settings.AppId}" +
                     $"&client_secret={_settings.AppSecret}" +
                     $"&fb_exchange_token={currentToken}";

            var response = await ExecuteWithRetryAsync<LongLivedTokenResponse>(
                () => _httpClient.GetAsync(url));

            _logger.LogInformation("Successfully refreshed long-lived token");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token");
            throw;
        }
    }

    /// <summary>
    /// Debug token to get information about validity, expiration, and permissions
    /// </summary>
    public async Task<TokenDebugData> DebugTokenAsync(string accessToken)
    {
        try
        {
            var url = $"{_settings.GraphApiBaseUrl}/debug_token" +
                     $"?input_token={accessToken}" +
                     $"&access_token={_settings.AppId}|{_settings.AppSecret}";

            var response = await ExecuteWithRetryAsync<TokenDebugResponse>(
                () => _httpClient.GetAsync(url));

            _logger.LogInformation("Token debug successful - Valid: {IsValid}, Expires: {ExpiresAt}",
                response.Data.IsValid,
                DateTimeOffset.FromUnixTimeSeconds(response.Data.ExpiresAt));

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to debug token");
            throw;
        }
    }

    /// <summary>
    /// Get Facebook Pages connected to the user
    /// </summary>
    public async Task<List<FacebookPage>> GetUserPagesAsync(string userAccessToken)
    {
        try
        {
            var url = $"{_settings.GraphApiBaseUrl}/me/accounts" +
                     $"?fields=id,name,access_token,instagram_business_account{{id,username,name,profile_picture_url}}" +
                     $"&access_token={userAccessToken}";

            var response = await ExecuteWithRetryAsync<FacebookPagesResponse>(
                () => _httpClient.GetAsync(url));

            _logger.LogInformation("Retrieved {Count} Facebook pages", response.Data.Count);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user pages");
            throw;
        }
    }

    /// <summary>
    /// Get Instagram Business Account connected to a Facebook Page
    /// </summary>
    public async Task<InstagramBusinessAccount?> GetInstagramBusinessAccountAsync(
        string pageId,
        string pageAccessToken)
    {
        try
        {
            var url = $"{_settings.GraphApiBaseUrl}/{pageId}" +
                     $"?fields=instagram_business_account{{id,username,name,profile_picture_url,followers_count,follows_count,media_count}}" +
                     $"&access_token={pageAccessToken}";

            var response = await ExecuteWithRetryAsync<FacebookPage>(
                () => _httpClient.GetAsync(url));

            _logger.LogInformation("Retrieved Instagram business account: {Username}",
                response.InstagramBusinessAccount?.Username ?? "N/A");

            return response.InstagramBusinessAccount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Instagram business account for page {PageId}", pageId);
            throw;
        }
    }

    /// <summary>
    /// Create a media container (Step 1 of publishing)
    /// </summary>
    public async Task<string> CreateMediaContainerAsync(
        string igUserId,
        string accessToken,
        CreateMediaContainerRequest request)
    {
        try
        {
            var url = $"{_settings.GraphApiBaseUrl}/{igUserId}/media";
            var parameters = BuildMediaContainerParameters(request);
            parameters.Add("access_token", accessToken);

            var content = new FormUrlEncodedContent(parameters);
            var response = await ExecuteWithRetryAsync<MediaContainerResponse>(
                () => _httpClient.PostAsync(url, content));

            _logger.LogInformation("Created media container: {ContainerId}, Status: {Status}",
                response.Id, response.Status ?? "N/A");

            return response.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create media container for IG user {IgUserId}", igUserId);
            throw;
        }
    }

    /// <summary>
    /// Publish a media container (Step 2 of publishing)
    /// </summary>
    public async Task<string> PublishMediaContainerAsync(
        string igUserId,
        string accessToken,
        string containerId)
    {
        try
        {
            var url = $"{_settings.GraphApiBaseUrl}/{igUserId}/media_publish";
            var parameters = new Dictionary<string, string>
            {
                { "creation_id", containerId },
                { "access_token", accessToken }
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await ExecuteWithRetryAsync<PublishMediaResponse>(
                () => _httpClient.PostAsync(url, content));

            _logger.LogInformation("Published media container {ContainerId}, Post ID: {PostId}",
                containerId, response.Id);

            return response.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish container {ContainerId}", containerId);
            throw;
        }
    }

    /// <summary>
    /// Check the status of a media container (useful for video processing)
    /// </summary>
    public async Task<ContainerStatusResponse> GetContainerStatusAsync(
        string containerId,
        string accessToken)
    {
        try
        {
            var url = $"{_settings.GraphApiBaseUrl}/{containerId}" +
                     $"?fields=id,status,status_code" +
                     $"&access_token={accessToken}";

            var response = await ExecuteWithRetryAsync<ContainerStatusResponse>(
                () => _httpClient.GetAsync(url));

            _logger.LogInformation("Container {ContainerId} status: {Status}",
                containerId, response.Status);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container status for {ContainerId}", containerId);
            throw;
        }
    }

    /// <summary>
    /// Get insights/analytics for a published media
    /// </summary>
    public async Task<InstagramMediaInsights> GetMediaInsightsAsync(
        string mediaId,
        string accessToken,
        string[] metrics)
    {
        try
        {
            var metricsParam = string.Join(",", metrics);
            var url = $"{_settings.GraphApiBaseUrl}/{mediaId}/insights" +
                     $"?metric={metricsParam}" +
                     $"&access_token={accessToken}";

            var response = await ExecuteWithRetryAsync<InstagramMediaInsights>(
                () => _httpClient.GetAsync(url));

            _logger.LogInformation("Retrieved insights for media {MediaId}: {Count} metrics",
                mediaId, response.Data.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media insights for {MediaId}", mediaId);
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Execute HTTP request with retry logic
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<HttpResponseMessage>> httpCall)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt < _settings.MaxRetryAttempts; attempt++)
        {
            try
            {
                var response = await httpCall();
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var error = JsonSerializer.Deserialize<InstagramErrorResponse>(content);

                    // Check if error is retryable
                    if (error?.Error != null && IsRetryableError(error.Error.Code))
                    {
                        _logger.LogWarning("Retryable error {Code}: {Message}. Attempt {Attempt}/{Max}",
                            error.Error.Code, error.Error.Message, attempt + 1, _settings.MaxRetryAttempts);

                        if (attempt < _settings.MaxRetryAttempts - 1)
                        {
                            await Task.Delay(_settings.RetryDelayMs * (attempt + 1));
                            continue;
                        }
                    }

                    throw new InstagramApiException(
                        error?.Error.Message ?? "Unknown error",
                        error?.Error.Code ?? 0,
                        error?.Error.ErrorSubcode,
                        content);
                }

                var result = JsonSerializer.Deserialize<T>(content);
                if (result == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize response to {typeof(T).Name}");
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "HTTP request failed. Attempt {Attempt}/{Max}",
                    attempt + 1, _settings.MaxRetryAttempts);

                if (attempt < _settings.MaxRetryAttempts - 1)
                {
                    await Task.Delay(_settings.RetryDelayMs * (attempt + 1));
                    continue;
                }
            }
            catch (InstagramApiException)
            {
                throw; // Don't retry API errors (except retryable ones handled above)
            }
        }

        throw new InvalidOperationException(
            $"Failed after {_settings.MaxRetryAttempts} attempts",
            lastException);
    }

    /// <summary>
    /// Check if an error code is retryable
    /// </summary>
    private bool IsRetryableError(int errorCode)
    {
        return errorCode switch
        {
            InstagramErrorCodes.RateLimitExceeded => true,
            InstagramErrorCodes.TooManyCalls => true,
            InstagramErrorCodes.UserRequestLimitReached => true,
            InstagramErrorCodes.ApplicationRequestLimitReached => true,
            _ => false
        };
    }

    /// <summary>
    /// Build parameters for creating media container
    /// </summary>
    private Dictionary<string, string> BuildMediaContainerParameters(CreateMediaContainerRequest request)
    {
        var parameters = new Dictionary<string, string>();

        // Media URL (required for non-carousel containers)
        if (!string.IsNullOrEmpty(request.MediaUrl))
        {
            var paramName = request.MediaType == InstagramMediaType.VIDEO ||
                           request.MediaType == InstagramMediaType.REELS
                ? "video_url"
                : "image_url";
            parameters.Add(paramName, request.MediaUrl);
        }

        // Caption
        if (!string.IsNullOrEmpty(request.Caption))
        {
            if (request.Caption.Length > InstagramMediaConstraints.MaxCaptionLength)
            {
                throw new ArgumentException(
                    $"Caption exceeds maximum length of {InstagramMediaConstraints.MaxCaptionLength} characters");
            }
            parameters.Add("caption", request.Caption);
        }

        // Media type
        if (request.MediaType != InstagramMediaType.IMAGE)
        {
            parameters.Add("media_type", request.MediaType.ToString());
        }

        // Carousel item flag
        if (request.IsCarouselItem)
        {
            parameters.Add("is_carousel_item", "true");
        }

        // Children (for carousel)
        if (!string.IsNullOrEmpty(request.Children))
        {
            parameters.Add("children", request.Children);
        }

        // Location
        if (!string.IsNullOrEmpty(request.LocationId))
        {
            parameters.Add("location_id", request.LocationId);
        }

        // User tags
        if (!string.IsNullOrEmpty(request.UserTags))
        {
            parameters.Add("user_tags", request.UserTags);
        }

        // Product tags
        if (!string.IsNullOrEmpty(request.ProductTags))
        {
            parameters.Add("product_tags", request.ProductTags);
        }

        // Thumbnail offset (for videos/reels)
        if (request.ThumbOffset.HasValue)
        {
            parameters.Add("thumb_offset", request.ThumbOffset.Value.ToString());
        }

        // Share to feed (for reels)
        if (request.ShareToFeed.HasValue)
        {
            parameters.Add("share_to_feed", request.ShareToFeed.Value.ToString().ToLower());
        }

        // Cover URL (for videos/reels)
        if (!string.IsNullOrEmpty(request.CoverUrl))
        {
            parameters.Add("cover_url", request.CoverUrl);
        }

        return parameters;
    }

    #endregion
}

/// <summary>
/// Custom exception for Instagram API errors
/// </summary>
public class InstagramApiException : Exception
{
    public int ErrorCode { get; }
    public int? ErrorSubcode { get; }
    public string RawResponse { get; }

    public InstagramApiException(string message, int errorCode, int? errorSubcode, string rawResponse)
        : base(message)
    {
        ErrorCode = errorCode;
        ErrorSubcode = errorSubcode;
        RawResponse = rawResponse;
    }
}
