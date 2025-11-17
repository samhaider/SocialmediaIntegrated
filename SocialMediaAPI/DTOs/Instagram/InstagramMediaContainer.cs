using System.Text.Json.Serialization;

namespace SocialMediaAPI.DTOs.Instagram;

/// <summary>
/// Supported Instagram media types
/// </summary>
public enum InstagramMediaType
{
    IMAGE,
    VIDEO,
    REELS,
    CAROUSEL
}

/// <summary>
/// Request to create an Instagram media container
/// </summary>
public class CreateMediaContainerRequest
{
    /// <summary>
    /// URL of the image or video (must be publicly accessible)
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>
    /// Caption for the post (max 2200 characters)
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Media type (IMAGE, VIDEO, REELS, CAROUSEL)
    /// </summary>
    public InstagramMediaType MediaType { get; set; } = InstagramMediaType.IMAGE;

    /// <summary>
    /// For carousel items only
    /// </summary>
    public bool IsCarouselItem { get; set; }

    /// <summary>
    /// Comma-separated container IDs for carousel
    /// </summary>
    public string? Children { get; set; }

    /// <summary>
    /// Location ID for tagging location
    /// </summary>
    public string? LocationId { get; set; }

    /// <summary>
    /// User tags (JSON array of {username, x, y})
    /// </summary>
    public string? UserTags { get; set; }

    /// <summary>
    /// Product tags for shopping posts
    /// </summary>
    public string? ProductTags { get; set; }

    /// <summary>
    /// Video thumbnail offset in milliseconds (for videos/reels)
    /// </summary>
    public int? ThumbOffset { get; set; }

    /// <summary>
    /// Share reel to feed (for REELS only)
    /// </summary>
    public bool? ShareToFeed { get; set; }

    /// <summary>
    /// Cover image URL for videos/reels
    /// </summary>
    public string? CoverUrl { get; set; }
}

/// <summary>
/// Response from creating a media container
/// </summary>
public class MediaContainerResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("status_code")]
    public string? StatusCode { get; set; }
}

/// <summary>
/// Response from publishing a media container
/// </summary>
public class PublishMediaResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Container status check response
/// </summary>
public class ContainerStatusResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("status_code")]
    public string? StatusCode { get; set; }
}

/// <summary>
/// Instagram media insights/analytics
/// </summary>
public class InstagramMediaInsights
{
    [JsonPropertyName("data")]
    public List<InsightData> Data { get; set; } = new();
}

public class InsightData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public List<InsightValue> Values { get; set; } = new();

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class InsightValue
{
    [JsonPropertyName("value")]
    public int Value { get; set; }
}
