namespace SocialMediaAPI.DTOs;

/// <summary>
/// Request to initiate Reddit OAuth flow
/// </summary>
public class RedditOAuthInitRequest
{
    public string RedirectUri { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// Response containing Reddit OAuth authorization URL
/// </summary>
public class RedditOAuthInitResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// Request to exchange authorization code for tokens
/// </summary>
public class RedditOAuthCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}

/// <summary>
/// Reddit token response from OAuth
/// </summary>
public class RedditTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a Reddit post
/// </summary>
public class RedditPostRequest
{
    public string Subreddit { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public RedditPostKind Kind { get; set; } = RedditPostKind.Self;
    public string? Text { get; set; }
    public string? Url { get; set; }
    public bool Nsfw { get; set; } = false;
    public bool Spoiler { get; set; } = false;
    public bool SendReplies { get; set; } = true;
    public string? FlairId { get; set; }
    public string? FlairText { get; set; }
}

/// <summary>
/// Types of Reddit posts
/// </summary>
public enum RedditPostKind
{
    Self,    // Text post
    Link,    // Link post
    Image,   // Image post
    Video    // Video post (limited support)
}

/// <summary>
/// Request to create a Reddit comment
/// </summary>
public class RedditCommentRequest
{
    public string ThingId { get; set; } = string.Empty; // Full name of parent (e.g., t3_xxx for post)
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Reddit post submission response
/// </summary>
public class RedditSubmitResponse
{
    public bool Success { get; set; }
    public RedditSubmitData? Data { get; set; }
}

public class RedditSubmitData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Reddit comment response
/// </summary>
public class RedditCommentResponse
{
    public bool Success { get; set; }
    public string CommentId { get; set; } = string.Empty;
}

/// <summary>
/// Reddit media asset upload request
/// </summary>
public class RedditMediaAssetRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
}

/// <summary>
/// Reddit media upload response
/// </summary>
public class RedditMediaAssetResponse
{
    public RedditMediaAssetData? Asset { get; set; }
}

public class RedditMediaAssetData
{
    public string AssetId { get; set; } = string.Empty;
    public string ProcessingState { get; set; } = string.Empty;
    public RedditMediaAssetPayload? Payload { get; set; }
}

public class RedditMediaAssetPayload
{
    public string Action { get; set; } = string.Empty;
    public List<RedditMediaAssetField> Fields { get; set; } = new();
}

public class RedditMediaAssetField
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Reddit user info (from /api/v1/me)
/// </summary>
public class RedditUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int LinkKarma { get; set; }
    public int CommentKarma { get; set; }
    public bool IsGold { get; set; }
    public bool IsMod { get; set; }
}

/// <summary>
/// Reddit API error response
/// </summary>
public class RedditErrorResponse
{
    public int ErrorCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
