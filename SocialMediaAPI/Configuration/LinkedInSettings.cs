namespace SocialMediaAPI.Configuration;

public class LinkedInSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public RateLimitingSettings RateLimiting { get; set; } = new();
}

public class RateLimitingSettings
{
    public int MaxRequestsPerMinute { get; set; }
    public int MaxRequestsPerDay { get; set; }
}
