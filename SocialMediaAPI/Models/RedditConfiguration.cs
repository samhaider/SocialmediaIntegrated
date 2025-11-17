namespace SocialMediaAPI.Models;

/// <summary>
/// Reddit API configuration settings
/// </summary>
public class RedditConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "SocialMediaAPI/1.0";

    /// <summary>
    /// Default redirect URI for OAuth callbacks
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Scopes required for Reddit API access
    /// Common scopes: identity, read, submit, edit, modconfig
    /// </summary>
    public string Scopes { get; set; } = "identity,read,submit,edit";

    /// <summary>
    /// Reddit OAuth authorization endpoint
    /// </summary>
    public string AuthorizationEndpoint { get; set; } = "https://www.reddit.com/api/v1/authorize";

    /// <summary>
    /// Reddit OAuth token endpoint
    /// </summary>
    public string TokenEndpoint { get; set; } = "https://www.reddit.com/api/v1/access_token";

    /// <summary>
    /// Reddit API base URL (OAuth)
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://oauth.reddit.com";

    /// <summary>
    /// Rate limit: requests per minute (Reddit recommends 60)
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 60;
}
