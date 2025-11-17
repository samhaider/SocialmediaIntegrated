using System.Text.Json.Serialization;

namespace SocialMediaAPI.DTOs.Instagram;

/// <summary>
/// Instagram Graph API error response
/// </summary>
public class InstagramErrorResponse
{
    [JsonPropertyName("error")]
    public InstagramError Error { get; set; } = new();
}

public class InstagramError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("error_subcode")]
    public int? ErrorSubcode { get; set; }

    [JsonPropertyName("error_user_title")]
    public string? ErrorUserTitle { get; set; }

    [JsonPropertyName("error_user_msg")]
    public string? ErrorUserMsg { get; set; }

    [JsonPropertyName("fbtrace_id")]
    public string? FbTraceId { get; set; }
}

/// <summary>
/// Common Instagram API error codes
/// </summary>
public static class InstagramErrorCodes
{
    public const int InvalidAccessToken = 190;
    public const int PermissionDenied = 10;
    public const int RateLimitExceeded = 4;
    public const int UserRequestLimitReached = 17;
    public const int ApplicationRequestLimitReached = 4;
    public const int InvalidParameter = 100;
    public const int InvalidOAuthAccessToken = 190;
    public const int AccessTokenExpired = 463;
    public const int PasswordChanged = 464;
    public const int SessionExpired = 467;
    public const int TooManyCalls = 32;
}

/// <summary>
/// Instagram media validation constraints
/// </summary>
public static class InstagramMediaConstraints
{
    // Image constraints
    public const int MaxCaptionLength = 2200;
    public const int MaxImageSizeMB = 8;
    public const int MinImageWidth = 320;
    public const int MaxImageWidth = 1440;
    public const double MinAspectRatio = 0.8; // 4:5
    public const double MaxAspectRatio = 1.91; // 1.91:1

    // Video constraints
    public const int MaxVideoSizeMB = 100;
    public const int MinVideoDurationSeconds = 3;
    public const int MaxVideoDurationSeconds = 60;
    public const int MaxReelsDurationSeconds = 90;
    public const int MinVideoWidth = 320;
    public const int MaxVideoWidth = 1920;

    // Carousel constraints
    public const int MaxCarouselItems = 10;
    public const int MinCarouselItems = 2;

    // Supported formats
    public static readonly string[] SupportedImageFormats = { ".jpg", ".jpeg", ".png" };
    public static readonly string[] SupportedVideoFormats = { ".mp4", ".mov" };
    public static readonly string[] SupportedVideoCodecs = { "H.264", "VP8" };
    public static readonly string[] SupportedAudioCodecs = { "AAC", "Vorbis" };
}
