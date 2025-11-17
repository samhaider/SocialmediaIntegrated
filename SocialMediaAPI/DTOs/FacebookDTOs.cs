using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SocialMediaAPI.DTOs;

/// <summary>
/// Request to initiate Facebook OAuth flow - returns the authorization URL
/// </summary>
public class FacebookAuthUrlRequest
{
    /// <summary>
    /// State parameter for CSRF protection (optional but recommended)
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Custom redirect URI (optional - uses default from config if not provided)
    /// </summary>
    public string? RedirectUri { get; set; }
}

/// <summary>
/// Response containing Facebook OAuth authorization URL
/// </summary>
public class FacebookAuthUrlResponse
{
    /// <summary>
    /// The URL to redirect the user to for Facebook authentication
    /// </summary>
    public string AuthorizationUrl { get; set; } = string.Empty;

    /// <summary>
    /// State parameter to validate on callback
    /// </summary>
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// Request to exchange OAuth code for access token
/// </summary>
public class FacebookOAuthCallbackRequest
{
    /// <summary>
    /// The authorization code returned by Facebook
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// State parameter for CSRF validation
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Redirect URI used in the authorization request (must match exactly)
    /// </summary>
    public string? RedirectUri { get; set; }
}

/// <summary>
/// Response after successful OAuth token exchange
/// </summary>
public class FacebookOAuthCallbackResponse
{
    /// <summary>
    /// User access token (short-lived)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (usually "bearer")
    /// </summary>
    public string TokenType { get; set; } = "bearer";

    /// <summary>
    /// Seconds until token expiration
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// List of Facebook Pages the user manages
    /// </summary>
    public List<FacebookPageInfo> Pages { get; set; } = new();
}

/// <summary>
/// Information about a Facebook Page
/// </summary>
public class FacebookPageInfo
{
    /// <summary>
    /// Facebook Page ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Page display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Page-specific access token (long-lived)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Category of the page (e.g., "Business", "Brand", "Community")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// User's role on the page (e.g., "ADMINISTRATOR", "EDITOR")
    /// </summary>
    public List<string> Tasks { get; set; } = new();
}

/// <summary>
/// Request to connect a specific Facebook Page to user account
/// </summary>
public class ConnectFacebookPageRequest
{
    /// <summary>
    /// The Facebook Page ID to connect
    /// </summary>
    [Required]
    public string PageId { get; set; } = string.Empty;

    /// <summary>
    /// Page access token
    /// </summary>
    [Required]
    public string PageAccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Page name
    /// </summary>
    [Required]
    public string PageName { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata (JSON string)
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Facebook Graph API error response
/// </summary>
public class FacebookError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("error_subcode")]
    public int? ErrorSubcode { get; set; }

    [JsonPropertyName("fbtrace_id")]
    public string? FbTraceId { get; set; }
}

/// <summary>
/// Facebook Graph API error wrapper
/// </summary>
public class FacebookErrorResponse
{
    [JsonPropertyName("error")]
    public FacebookError Error { get; set; } = new();
}

/// <summary>
/// Facebook token exchange response
/// </summary>
public class FacebookTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }
}

/// <summary>
/// Facebook user accounts (pages) response
/// </summary>
public class FacebookAccountsResponse
{
    [JsonPropertyName("data")]
    public List<FacebookPageData> Data { get; set; } = new();

    [JsonPropertyName("paging")]
    public FacebookPaging? Paging { get; set; }
}

/// <summary>
/// Facebook Page data from /me/accounts endpoint
/// </summary>
public class FacebookPageData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("category_list")]
    public List<FacebookCategory>? CategoryList { get; set; }

    [JsonPropertyName("tasks")]
    public List<string>? Tasks { get; set; }

    [JsonPropertyName("perms")]
    public List<string>? Perms { get; set; }
}

/// <summary>
/// Facebook category information
/// </summary>
public class FacebookCategory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Facebook paging information
/// </summary>
public class FacebookPaging
{
    [JsonPropertyName("cursors")]
    public FacebookCursors? Cursors { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

/// <summary>
/// Facebook pagination cursors
/// </summary>
public class FacebookCursors
{
    [JsonPropertyName("before")]
    public string? Before { get; set; }

    [JsonPropertyName("after")]
    public string? After { get; set; }
}

/// <summary>
/// Response after posting to Facebook
/// </summary>
public class FacebookPostResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("post_id")]
    public string? PostId { get; set; }
}

/// <summary>
/// Request to create a post on Facebook Page
/// </summary>
public class FacebookCreatePostRequest
{
    /// <summary>
    /// Post message/content
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional link to share
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// Whether to publish immediately (true) or schedule for later (false)
    /// </summary>
    public bool Published { get; set; } = true;

    /// <summary>
    /// Unix timestamp for scheduled posts (required if Published = false)
    /// </summary>
    public long? ScheduledPublishTime { get; set; }
}
