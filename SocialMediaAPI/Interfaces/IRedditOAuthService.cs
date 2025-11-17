using SocialMediaAPI.DTOs;

namespace SocialMediaAPI.Interfaces;

/// <summary>
/// Service for handling Reddit OAuth 2.0 authentication flow
/// </summary>
public interface IRedditOAuthService
{
    /// <summary>
    /// Generate Reddit OAuth authorization URL
    /// </summary>
    /// <param name="redirectUri">The redirect URI registered with your Reddit app</param>
    /// <param name="state">Random state string for CSRF protection</param>
    /// <param name="scopes">Optional custom scopes (defaults to config scopes)</param>
    /// <returns>Authorization URL to redirect user to</returns>
    string GetAuthorizationUrl(string redirectUri, string state, string? scopes = null);

    /// <summary>
    /// Exchange authorization code for access token and refresh token
    /// </summary>
    /// <param name="code">Authorization code from callback</param>
    /// <param name="redirectUri">Same redirect URI used in authorization request</param>
    /// <returns>Token response with access token and refresh token</returns>
    Task<RedditTokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri);

    /// <summary>
    /// Refresh an expired access token using refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token</param>
    /// <returns>New token response with refreshed access token</returns>
    Task<RedditTokenResponse> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Validate an access token by fetching user info
    /// </summary>
    /// <param name="accessToken">The access token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
    Task<bool> ValidateTokenAsync(string accessToken);

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <param name="accessToken">Valid access token</param>
    /// <returns>Reddit user information</returns>
    Task<RedditUserInfo> GetUserInfoAsync(string accessToken);

    /// <summary>
    /// Revoke an access or refresh token
    /// </summary>
    /// <param name="token">Token to revoke</param>
    /// <param name="tokenTypeHint">"access_token" or "refresh_token"</param>
    /// <returns>True if revocation successful</returns>
    Task<bool> RevokeTokenAsync(string token, string tokenTypeHint = "access_token");
}
