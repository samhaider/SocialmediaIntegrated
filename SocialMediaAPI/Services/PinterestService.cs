using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Services;

/// <summary>
/// Service for Pinterest API v5 integration
/// Handles OAuth 2.0 authentication, pin creation, board management, and analytics
/// </summary>
public class PinterestService : IPinterestService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PinterestService> _logger;
    private const string API_BASE_URL = "https://api.pinterest.com/v5";
    private const string API_SANDBOX_URL = "https://api-sandbox.pinterest.com/v5";

    public PinterestService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PinterestService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets the API base URL based on environment configuration
    /// </summary>
    private string GetApiBaseUrl()
    {
        var useSandbox = _configuration.GetValue<bool>("Pinterest:UseSandbox");
        return useSandbox ? API_SANDBOX_URL : API_BASE_URL;
    }

    /// <summary>
    /// Validates the access token by attempting to fetch user account information
    /// </summary>
    public async Task<bool> ValidateAccessTokenAsync(string accessToken)
    {
        try
        {
            var userInfo = await GetUserAccountAsync(accessToken);
            return userInfo != null && !string.IsNullOrEmpty(userInfo.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Pinterest access token");
            return false;
        }
    }

    /// <summary>
    /// Refreshes an expired access token using the refresh token
    /// </summary>
    public async Task<PinterestTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var clientId = _configuration["Pinterest:ClientId"];
            var clientSecret = _configuration["Pinterest:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Pinterest ClientId or ClientSecret not configured");
                return null;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.pinterest.com/v5/oauth/token");

            // Set Basic Authentication header
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            // Set form data
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            });

            request.Content = formContent;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to refresh Pinterest token. Status: {Status}, Error: {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<PinterestTokenResponse>(content);

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Pinterest access token");
            return null;
        }
    }

    /// <summary>
    /// Gets the authenticated user's account information
    /// </summary>
    public async Task<PinterestUserResponse?> GetUserAccountAsync(string accessToken)
    {
        try
        {
            var baseUrl = GetApiBaseUrl();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/user_account");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get Pinterest user account. Status: {Status}, Error: {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var userResponse = JsonSerializer.Deserialize<PinterestUserResponse>(content);

            return userResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest user account");
            return null;
        }
    }

    /// <summary>
    /// Lists all boards for the authenticated user
    /// </summary>
    public async Task<PinterestBoardsResponse?> GetBoardsAsync(string accessToken, string? pageSize = null, string? bookmark = null)
    {
        try
        {
            var baseUrl = GetApiBaseUrl();
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(pageSize))
                queryParams.Add($"page_size={pageSize}");

            if (!string.IsNullOrEmpty(bookmark))
                queryParams.Add($"bookmark={bookmark}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var url = $"{baseUrl}/boards{queryString}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get Pinterest boards. Status: {Status}, Error: {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var boardsResponse = JsonSerializer.Deserialize<PinterestBoardsResponse>(content);

            return boardsResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest boards");
            return null;
        }
    }

    /// <summary>
    /// Creates a new pin on a specified board
    /// </summary>
    public async Task<PinterestPin?> CreatePinAsync(string accessToken, PinterestCreatePinRequest request)
    {
        try
        {
            var baseUrl = GetApiBaseUrl();
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/pins");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Creating Pinterest pin on board {BoardId}", request.BoardId);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create Pinterest pin. Status: {Status}, Error: {Error}",
                    response.StatusCode, responseContent);

                // Try to parse error response
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<PinterestErrorResponse>(responseContent);
                    throw new InvalidOperationException($"Pinterest API Error: {errorResponse?.Message ?? responseContent}");
                }
                catch (JsonException)
                {
                    throw new InvalidOperationException($"Pinterest API Error: {responseContent}");
                }
            }

            var pin = JsonSerializer.Deserialize<PinterestPin>(responseContent);
            _logger.LogInformation("Successfully created Pinterest pin with ID: {PinId}", pin?.Id);

            return pin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Pinterest pin");
            throw;
        }
    }

    /// <summary>
    /// Gets details of a specific pin
    /// </summary>
    public async Task<PinterestPin?> GetPinAsync(string accessToken, string pinId)
    {
        try
        {
            var baseUrl = GetApiBaseUrl();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/pins/{pinId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get Pinterest pin {PinId}. Status: {Status}, Error: {Error}",
                    pinId, response.StatusCode, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var pin = JsonSerializer.Deserialize<PinterestPin>(content);

            return pin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest pin {PinId}", pinId);
            return null;
        }
    }

    /// <summary>
    /// Lists all pins for the authenticated user
    /// </summary>
    public async Task<PinterestPinsResponse?> GetUserPinsAsync(string accessToken, string? pageSize = null, string? bookmark = null)
    {
        try
        {
            var baseUrl = GetApiBaseUrl();
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(pageSize))
                queryParams.Add($"page_size={pageSize}");

            if (!string.IsNullOrEmpty(bookmark))
                queryParams.Add($"bookmark={bookmark}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var url = $"{baseUrl}/pins{queryString}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get Pinterest user pins. Status: {Status}, Error: {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var pinsResponse = JsonSerializer.Deserialize<PinterestPinsResponse>(content);

            return pinsResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest user pins");
            return null;
        }
    }

    /// <summary>
    /// Lists pins on a specific board
    /// </summary>
    public async Task<PinterestPinsResponse?> GetBoardPinsAsync(string accessToken, string boardId, string? pageSize = null, string? bookmark = null)
    {
        try
        {
            var baseUrl = GetApiBaseUrl();
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(pageSize))
                queryParams.Add($"page_size={pageSize}");

            if (!string.IsNullOrEmpty(bookmark))
                queryParams.Add($"bookmark={bookmark}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var url = $"{baseUrl}/boards/{boardId}/pins{queryString}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get Pinterest board pins. Status: {Status}, Error: {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var pinsResponse = JsonSerializer.Deserialize<PinterestPinsResponse>(content);

            return pinsResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest board pins for board {BoardId}", boardId);
            return null;
        }
    }

    /// <summary>
    /// Gets analytics for a specific pin
    /// </summary>
    public async Task<PinterestPinAnalytics?> GetPinAnalyticsAsync(string accessToken, string pinId, string startDate, string endDate)
    {
        try
        {
            var baseUrl = GetApiBaseUrl();
            var metricTypes = new[] { "IMPRESSION", "SAVE", "PIN_CLICK", "OUTBOUND_CLICK", "VIDEO_MRC_VIEW" };
            var metricsParam = string.Join(",", metricTypes);

            var url = $"{baseUrl}/pins/{pinId}/analytics?start_date={startDate}&end_date={endDate}&metric_types={metricsParam}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get Pinterest pin analytics. Status: {Status}, Error: {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();

            // Pinterest returns analytics in a nested structure, we need to parse it carefully
            var jsonDoc = JsonDocument.Parse(content);
            var analytics = new PinterestPinAnalytics();

            if (jsonDoc.RootElement.TryGetProperty("all", out var allElement))
            {
                if (allElement.TryGetProperty("daily_metrics", out var dailyMetrics) && dailyMetrics.GetArrayLength() > 0)
                {
                    var firstDay = dailyMetrics[0];

                    if (firstDay.TryGetProperty("data_status", out var dataStatus) &&
                        dataStatus.GetString() == "READY")
                    {
                        if (firstDay.TryGetProperty("metrics", out var metrics))
                        {
                            if (metrics.TryGetProperty("IMPRESSION", out var impression))
                                analytics.Impressions = impression.GetInt32();

                            if (metrics.TryGetProperty("SAVE", out var save))
                                analytics.Saves = save.GetInt32();

                            if (metrics.TryGetProperty("PIN_CLICK", out var pinClick))
                                analytics.PinClicks = pinClick.GetInt32();

                            if (metrics.TryGetProperty("OUTBOUND_CLICK", out var outboundClick))
                                analytics.OutboundClicks = outboundClick.GetInt32();

                            if (metrics.TryGetProperty("VIDEO_MRC_VIEW", out var videoView))
                                analytics.VideoViews = videoView.GetInt32();
                        }
                    }
                }
            }

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pinterest pin analytics for pin {PinId}", pinId);
            return null;
        }
    }

    /// <summary>
    /// Deletes a pin
    /// </summary>
    public async Task<bool> DeletePinAsync(string accessToken, string pinId)
    {
        try
        {
            var baseUrl = GetApiBaseUrl();
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/pins/{pinId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete Pinterest pin {PinId}. Status: {Status}, Error: {Error}",
                    pinId, response.StatusCode, errorContent);
                return false;
            }

            _logger.LogInformation("Successfully deleted Pinterest pin {PinId}", pinId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Pinterest pin {PinId}", pinId);
            return false;
        }
    }
}
