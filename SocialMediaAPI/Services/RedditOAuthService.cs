using Microsoft.Extensions.Options;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SocialMediaAPI.Services;

/// <summary>
/// Implementation of Reddit OAuth 2.0 authentication service
/// Handles authorization flow, token management, and token refresh
/// </summary>
public class RedditOAuthService : IRedditOAuthService
{
    private readonly RedditConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RedditOAuthService> _logger;

    public RedditOAuthService(
        IOptions<RedditConfiguration> config,
        IHttpClientFactory httpClientFactory,
        ILogger<RedditOAuthService> logger)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string GetAuthorizationUrl(string redirectUri, string state, string? scopes = null)
    {
        var scopeList = scopes ?? _config.Scopes;

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _config.ClientId,
            ["response_type"] = "code",
            ["state"] = state,
            ["redirect_uri"] = redirectUri,
            ["duration"] = "permanent", // Request permanent access (includes refresh token)
            ["scope"] = scopeList
        };

        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{_config.AuthorizationEndpoint}?{queryString}";
    }

    public async Task<RedditTokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        try
        {
            var client = CreateHttpClient();

            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            };

            var response = await client.PostAsync(_config.TokenEndpoint,
                new FormUrlEncodedContent(requestData));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to exchange code for token. Status: {Status}, Response: {Response}",
                    response.StatusCode, content);
                throw new HttpRequestException($"Token exchange failed: {response.StatusCode}");
            }

            var tokenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);

            if (tokenData == null)
            {
                throw new InvalidOperationException("Failed to parse token response");
            }

            return new RedditTokenResponse
            {
                AccessToken = tokenData["access_token"].GetString() ?? string.Empty,
                TokenType = tokenData["token_type"].GetString() ?? "bearer",
                ExpiresIn = tokenData["expires_in"].GetInt32(),
                RefreshToken = tokenData.ContainsKey("refresh_token")
                    ? tokenData["refresh_token"].GetString() ?? string.Empty
                    : string.Empty,
                Scope = tokenData.ContainsKey("scope")
                    ? tokenData["scope"].GetString() ?? string.Empty
                    : string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for token");
            throw;
        }
    }

    public async Task<RedditTokenResponse> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var client = CreateHttpClient();

            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            };

            var response = await client.PostAsync(_config.TokenEndpoint,
                new FormUrlEncodedContent(requestData));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to refresh token. Status: {Status}, Response: {Response}",
                    response.StatusCode, content);
                throw new HttpRequestException($"Token refresh failed: {response.StatusCode}");
            }

            var tokenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);

            if (tokenData == null)
            {
                throw new InvalidOperationException("Failed to parse token response");
            }

            return new RedditTokenResponse
            {
                AccessToken = tokenData["access_token"].GetString() ?? string.Empty,
                TokenType = tokenData["token_type"].GetString() ?? "bearer",
                ExpiresIn = tokenData["expires_in"].GetInt32(),
                RefreshToken = refreshToken, // Refresh token stays the same
                Scope = tokenData.ContainsKey("scope")
                    ? tokenData["scope"].GetString() ?? string.Empty
                    : string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing access token");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string accessToken)
    {
        try
        {
            var userInfo = await GetUserInfoAsync(accessToken);
            return !string.IsNullOrEmpty(userInfo.Id);
        }
        catch
        {
            return false;
        }
    }

    public async Task<RedditUserInfo> GetUserInfoAsync(string accessToken)
    {
        try
        {
            var client = CreateOAuthHttpClient(accessToken);

            var response = await client.GetAsync($"{_config.ApiBaseUrl}/api/v1/me");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get user info. Status: {Status}, Response: {Response}",
                    response.StatusCode, content);
                throw new HttpRequestException($"Get user info failed: {response.StatusCode}");
            }

            var userData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);

            if (userData == null)
            {
                throw new InvalidOperationException("Failed to parse user info response");
            }

            return new RedditUserInfo
            {
                Id = userData["id"].GetString() ?? string.Empty,
                Name = userData["name"].GetString() ?? string.Empty,
                LinkKarma = userData.ContainsKey("link_karma") ? userData["link_karma"].GetInt32() : 0,
                CommentKarma = userData.ContainsKey("comment_karma") ? userData["comment_karma"].GetInt32() : 0,
                IsGold = userData.ContainsKey("is_gold") && userData["is_gold"].GetBoolean(),
                IsMod = userData.ContainsKey("is_mod") && userData["is_mod"].GetBoolean()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info");
            throw;
        }
    }

    public async Task<bool> RevokeTokenAsync(string token, string tokenTypeHint = "access_token")
    {
        try
        {
            var client = CreateHttpClient();

            var requestData = new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = tokenTypeHint
            };

            var response = await client.PostAsync("https://www.reddit.com/api/v1/revoke_token",
                new FormUrlEncodedContent(requestData));

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return false;
        }
    }

    /// <summary>
    /// Create HTTP client with Basic Auth for token requests
    /// </summary>
    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_config.UserAgent);

        // Basic authentication with client_id:client_secret
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        return client;
    }

    /// <summary>
    /// Create HTTP client with OAuth Bearer token for API requests
    /// </summary>
    private HttpClient CreateOAuthHttpClient(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_config.UserAgent);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
