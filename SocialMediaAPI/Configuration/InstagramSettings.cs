namespace SocialMediaAPI.Configuration;

/// <summary>
/// Configuration settings for Instagram Graph API integration
/// </summary>
public class InstagramSettings
{
    /// <summary>
    /// Facebook App ID (required for Instagram API)
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Facebook App Secret (required for Instagram API)
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// OAuth redirect URI for Facebook Login
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Instagram Graph API base URL
    /// </summary>
    public string GraphApiBaseUrl { get; set; } = "https://graph.facebook.com/v21.0";

    /// <summary>
    /// Facebook OAuth base URL
    /// </summary>
    public string OAuthBaseUrl { get; set; } = "https://www.facebook.com/v21.0/dialog/oauth";

    /// <summary>
    /// Required permissions for Instagram publishing
    /// </summary>
    public string[] RequiredPermissions { get; set; } = new[]
    {
        "instagram_basic",
        "instagram_content_publish",
        "pages_show_list",
        "pages_read_engagement",
        "business_management"
    };

    /// <summary>
    /// Maximum retry attempts for API calls
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Token refresh threshold in days (refresh before expiration)
    /// </summary>
    public int TokenRefreshThresholdDays { get; set; } = 7;
}
