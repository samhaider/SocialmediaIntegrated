using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SocialMediaAPI.Configuration;
using SocialMediaAPI.DTOs;

namespace SocialMediaAPI.Services.LinkedIn;

public interface ILinkedInOAuthService
{
    string GetAuthorizationUrl(string state);
    Task<LinkedInTokenResponse> ExchangeCodeForTokenAsync(string code);
    Task<LinkedInTokenResponse> RefreshTokenAsync(string refreshToken);
    Task<LinkedInUserInfoResponse> GetUserInfoAsync(string accessToken);
    Task<List<LinkedInOrganization>> GetUserOrganizationsAsync(string accessToken);
    Task<bool> ValidateTokenAsync(string accessToken);
}

public class LinkedInOAuthService : ILinkedInOAuthService
{
    private readonly HttpClient _httpClient;
    private readonly LinkedInSettings _settings;
    private readonly ILogger<LinkedInOAuthService> _logger;

    public LinkedInOAuthService(
        HttpClient httpClient,
        IOptions<LinkedInSettings> settings,
        ILogger<LinkedInOAuthService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string GetAuthorizationUrl(string state)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "client_id", _settings.ClientId },
            { "redirect_uri", _settings.RedirectUri },
            { "scope", _settings.Scopes },
            { "state", state }
        };

        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{_settings.AuthorizationEndpoint}?{queryString}";
    }

    public async Task<LinkedInTokenResponse> ExchangeCodeForTokenAsync(string code)
    {
        try
        {
            var requestData = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", _settings.RedirectUri },
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret }
            };

            var content = new FormUrlEncodedContent(requestData);
            var response = await _httpClient.PostAsync(_settings.TokenEndpoint, content);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to exchange code for token. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"Token exchange failed: {response.StatusCode}");
            }

            var tokenResponse = JsonSerializer.Deserialize<LinkedInTokenResponse>(responseContent);
            if (tokenResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize token response");
            }

            _logger.LogInformation("Successfully exchanged authorization code for access token");
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for token");
            throw;
        }
    }

    public async Task<LinkedInTokenResponse> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var requestData = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret }
            };

            var content = new FormUrlEncodedContent(requestData);
            var response = await _httpClient.PostAsync(_settings.TokenEndpoint, content);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to refresh token. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"Token refresh failed: {response.StatusCode}");
            }

            var tokenResponse = JsonSerializer.Deserialize<LinkedInTokenResponse>(responseContent);
            if (tokenResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize token response");
            }

            _logger.LogInformation("Successfully refreshed access token");
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing access token");
            throw;
        }
    }

    public async Task<LinkedInUserInfoResponse> GetUserInfoAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/userinfo");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get user info. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"Get user info failed: {response.StatusCode}");
            }

            var userInfo = JsonSerializer.Deserialize<LinkedInUserInfoResponse>(responseContent);
            if (userInfo == null)
            {
                throw new InvalidOperationException("Failed to deserialize user info response");
            }

            _logger.LogInformation("Successfully retrieved user info for sub: {Sub}", userInfo.Sub);
            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info");
            throw;
        }
    }

    public async Task<List<LinkedInOrganization>> GetUserOrganizationsAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_settings.ApiBaseUrl}/organizationAcls?q=roleAssignee&projection=(elements*(organization~(localizedName),role,state))");

            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Headers.Add("LinkedIn-Version", _settings.ApiVersion);
            request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get user organizations. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);

                // If the user doesn't have access to any organizations, return empty list
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                    response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("User does not have access to any organizations");
                    return new List<LinkedInOrganization>();
                }

                throw new HttpRequestException($"Get organizations failed: {response.StatusCode}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var organizationsResponse = JsonSerializer.Deserialize<LinkedInOrganizationsResponse>(
                responseContent, options);

            if (organizationsResponse?.Elements == null)
            {
                _logger.LogWarning("No organizations found for user");
                return new List<LinkedInOrganization>();
            }

            _logger.LogInformation("Successfully retrieved {Count} organizations for user",
                organizationsResponse.Elements.Count);

            return organizationsResponse.Elements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user organizations");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string accessToken)
    {
        try
        {
            // Validate by attempting to get user info
            await GetUserInfoAsync(accessToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }
}
