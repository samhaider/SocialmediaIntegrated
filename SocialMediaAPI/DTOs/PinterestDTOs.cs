using System.Text.Json.Serialization;

namespace SocialMediaAPI.DTOs;

/// <summary>
/// Pinterest OAuth 2.0 token response
/// </summary>
public class PinterestTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;
}

/// <summary>
/// Pinterest OAuth 2.0 token refresh request
/// </summary>
public class PinterestTokenRefreshRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = "refresh_token";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Pinterest user account information
/// </summary>
public class PinterestUserResponse
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("account_type")]
    public string AccountType { get; set; } = string.Empty;

    [JsonPropertyName("profile_image")]
    public string? ProfileImage { get; set; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("monthly_views")]
    public int? MonthlyViews { get; set; }
}

/// <summary>
/// Pinterest board information
/// </summary>
public class PinterestBoard
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("privacy")]
    public string Privacy { get; set; } = string.Empty; // PUBLIC, PROTECTED, SECRET

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("pin_count")]
    public int? PinCount { get; set; }

    [JsonPropertyName("follower_count")]
    public int? FollowerCount { get; set; }
}

/// <summary>
/// Pinterest boards list response
/// </summary>
public class PinterestBoardsResponse
{
    [JsonPropertyName("items")]
    public List<PinterestBoard> Items { get; set; } = new();

    [JsonPropertyName("bookmark")]
    public string? Bookmark { get; set; }
}

/// <summary>
/// Pinterest board section information
/// </summary>
public class PinterestBoardSection
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Pinterest pin media source configuration
/// </summary>
public class PinterestMediaSource
{
    [JsonPropertyName("source_type")]
    public string SourceType { get; set; } = string.Empty; // image_url, image_base64, video_id

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; } // Base64 encoded image data
}

/// <summary>
/// Pinterest pin creation request
/// </summary>
public class PinterestCreatePinRequest
{
    [JsonPropertyName("board_id")]
    public string BoardId { get; set; } = string.Empty;

    [JsonPropertyName("board_section_id")]
    public string? BoardSectionId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("alt_text")]
    public string? AltText { get; set; }

    [JsonPropertyName("media_source")]
    public PinterestMediaSource MediaSource { get; set; } = new();
}

/// <summary>
/// Pinterest pin information
/// </summary>
public class PinterestPin
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("alt_text")]
    public string? AltText { get; set; }

    [JsonPropertyName("board_id")]
    public string BoardId { get; set; } = string.Empty;

    [JsonPropertyName("board_section_id")]
    public string? BoardSectionId { get; set; }

    [JsonPropertyName("media")]
    public PinterestMediaInfo? Media { get; set; }
}

/// <summary>
/// Pinterest pin media information
/// </summary>
public class PinterestMediaInfo
{
    [JsonPropertyName("media_type")]
    public string MediaType { get; set; } = string.Empty;

    [JsonPropertyName("images")]
    public Dictionary<string, PinterestImageInfo>? Images { get; set; }
}

/// <summary>
/// Pinterest image information
/// </summary>
public class PinterestImageInfo
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Pinterest pins list response
/// </summary>
public class PinterestPinsResponse
{
    [JsonPropertyName("items")]
    public List<PinterestPin> Items { get; set; } = new();

    [JsonPropertyName("bookmark")]
    public string? Bookmark { get; set; }
}

/// <summary>
/// Pinterest pin analytics request
/// </summary>
public class PinterestAnalyticsRequest
{
    [JsonPropertyName("start_date")]
    public string StartDate { get; set; } = string.Empty; // YYYY-MM-DD

    [JsonPropertyName("end_date")]
    public string EndDate { get; set; } = string.Empty; // YYYY-MM-DD

    [JsonPropertyName("metric_types")]
    public List<string> MetricTypes { get; set; } = new(); // IMPRESSION, SAVE, PIN_CLICK, etc.
}

/// <summary>
/// Pinterest pin analytics response
/// </summary>
public class PinterestPinAnalytics
{
    [JsonPropertyName("IMPRESSION")]
    public int? Impressions { get; set; }

    [JsonPropertyName("SAVE")]
    public int? Saves { get; set; }

    [JsonPropertyName("PIN_CLICK")]
    public int? PinClicks { get; set; }

    [JsonPropertyName("OUTBOUND_CLICK")]
    public int? OutboundClicks { get; set; }

    [JsonPropertyName("VIDEO_MRC_VIEW")]
    public int? VideoViews { get; set; }
}

/// <summary>
/// Pinterest error response
/// </summary>
public class PinterestErrorResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request to get Pinterest boards
/// </summary>
public class GetPinterestBoardsRequest
{
    public string? PageSize { get; set; }
    public string? Bookmark { get; set; }
}

/// <summary>
/// Request to create a Pinterest pin with board selection
/// </summary>
public class CreatePinterestPinRequest
{
    public string BoardId { get; set; } = string.Empty;
    public string? BoardSectionId { get; set; }
    public string? Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string? AltText { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = "image_url"; // image_url, image_base64, video_id
}
