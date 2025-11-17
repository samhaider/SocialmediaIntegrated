using System.Text.Json.Serialization;

namespace SocialMediaAPI.DTOs;

#region OAuth DTOs

public class LinkedInTokenRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = "authorization_code";

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }
}

public class LinkedInTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("refresh_token_expires_in")]
    public int? RefreshTokenExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

public class LinkedInUserInfoResponse
{
    [JsonPropertyName("sub")]
    public string Sub { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
}

#endregion

#region Organization DTOs

public class LinkedInOrganizationsResponse
{
    [JsonPropertyName("elements")]
    public List<LinkedInOrganization> Elements { get; set; } = new();

    [JsonPropertyName("paging")]
    public LinkedInPaging? Paging { get; set; }
}

public class LinkedInOrganization
{
    [JsonPropertyName("organization")]
    public string Organization { get; set; } = string.Empty; // URN format

    [JsonPropertyName("organizationName")]
    public string? OrganizationName { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // ADMINISTRATOR, CONTENT_ADMIN, etc.

    [JsonPropertyName("state")]
    public string? State { get; set; }
}

public class LinkedInOrganizationDetailsResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("localizedName")]
    public string? LocalizedName { get; set; }

    [JsonPropertyName("vanityName")]
    public string? VanityName { get; set; }

    [JsonPropertyName("localizedDescription")]
    public string? LocalizedDescription { get; set; }

    [JsonPropertyName("logoV2")]
    public LinkedInMediaReference? LogoV2 { get; set; }
}

#endregion

#region Posts API DTOs

public class LinkedInCreatePostRequest
{
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty; // URN: urn:li:organization:123456 or urn:li:person:ABC123

    [JsonPropertyName("commentary")]
    public string Commentary { get; set; } = string.Empty;

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "PUBLIC"; // PUBLIC, CONNECTIONS, LOGGED_IN

    [JsonPropertyName("distribution")]
    public LinkedInDistribution Distribution { get; set; } = new();

    [JsonPropertyName("content")]
    public LinkedInPostContent? Content { get; set; }

    [JsonPropertyName("lifecycleState")]
    public string LifecycleState { get; set; } = "PUBLISHED"; // PUBLISHED, DRAFT

    [JsonPropertyName("isReshareDisabledByAuthor")]
    public bool IsReshareDisabledByAuthor { get; set; } = false;
}

public class LinkedInDistribution
{
    [JsonPropertyName("feedDistribution")]
    public string FeedDistribution { get; set; } = "MAIN_FEED"; // MAIN_FEED, NONE

    [JsonPropertyName("targetEntities")]
    public List<string> TargetEntities { get; set; } = new();

    [JsonPropertyName("thirdPartyDistributionChannels")]
    public List<string> ThirdPartyDistributionChannels { get; set; } = new();
}

public class LinkedInPostContent
{
    [JsonPropertyName("media")]
    public LinkedInMediaContent? Media { get; set; }

    [JsonPropertyName("article")]
    public LinkedInArticleContent? Article { get; set; }
}

public class LinkedInMediaContent
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // Media asset URN
}

public class LinkedInArticleContent
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty; // URL

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; } // Media asset URN
}

public class LinkedInCreatePostResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // Post URN

    [JsonPropertyName("$URN")]
    public string? Urn { get; set; }
}

#endregion

#region Media Upload DTOs

public class LinkedInRegisterUploadRequest
{
    [JsonPropertyName("initializeUploadRequest")]
    public LinkedInInitializeUploadRequest InitializeUploadRequest { get; set; } = new();
}

public class LinkedInInitializeUploadRequest
{
    [JsonPropertyName("owner")]
    public string Owner { get; set; } = string.Empty; // Organization or person URN

    [JsonPropertyName("fileSizeBytes")]
    public long FileSizeBytes { get; set; }

    [JsonPropertyName("uploadCausalityDirection")]
    public string UploadCausalityDirection { get; set; } = "UPLOAD_NEW_MEDIA";

    [JsonPropertyName("recipes")]
    public List<string> Recipes { get; set; } = new();

    [JsonPropertyName("fileExtension")]
    public string? FileExtension { get; set; }
}

public class LinkedInRegisterUploadResponse
{
    [JsonPropertyName("value")]
    public LinkedInUploadValue Value { get; set; } = new();
}

public class LinkedInUploadValue
{
    [JsonPropertyName("uploadUrl")]
    public string? UploadUrl { get; set; }

    [JsonPropertyName("asset")]
    public string? Asset { get; set; } // Asset URN

    [JsonPropertyName("uploadInstructions")]
    public List<LinkedInUploadInstruction>? UploadInstructions { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; } // For image uploads

    [JsonPropertyName("video")]
    public string? Video { get; set; } // For video uploads
}

public class LinkedInUploadInstruction
{
    [JsonPropertyName("uploadUrl")]
    public string UploadUrl { get; set; } = string.Empty;

    [JsonPropertyName("firstByteIndex")]
    public long FirstByteIndex { get; set; }

    [JsonPropertyName("lastByteIndex")]
    public long LastByteIndex { get; set; }
}

#endregion

#region Social Actions DTOs

public class LinkedInSocialMetadataResponse
{
    [JsonPropertyName("elements")]
    public List<LinkedInSocialMetadata> Elements { get; set; } = new();
}

public class LinkedInSocialMetadata
{
    [JsonPropertyName("totalShareStatistics")]
    public LinkedInShareStatistics? TotalShareStatistics { get; set; }

    [JsonPropertyName("totalReactionStatistics")]
    public List<LinkedInReactionStatistic>? TotalReactionStatistics { get; set; }

    [JsonPropertyName("commentSummary")]
    public LinkedInCommentSummary? CommentSummary { get; set; }
}

public class LinkedInShareStatistics
{
    [JsonPropertyName("shareCount")]
    public int ShareCount { get; set; }

    [JsonPropertyName("commentCount")]
    public int CommentCount { get; set; }

    [JsonPropertyName("likeCount")]
    public int LikeCount { get; set; }

    [JsonPropertyName("impressionCount")]
    public int ImpressionCount { get; set; }

    [JsonPropertyName("engagement")]
    public double Engagement { get; set; }

    [JsonPropertyName("clickCount")]
    public int ClickCount { get; set; }
}

public class LinkedInReactionStatistic
{
    [JsonPropertyName("reactionType")]
    public string ReactionType { get; set; } = string.Empty; // LIKE, PRAISE, APPRECIATION, etc.

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class LinkedInCommentSummary
{
    [JsonPropertyName("totalFirstLevelComments")]
    public int TotalFirstLevelComments { get; set; }

    [JsonPropertyName("aggregatedTotalComments")]
    public int AggregatedTotalComments { get; set; }
}

public class LinkedInCommentsResponse
{
    [JsonPropertyName("elements")]
    public List<LinkedInComment> Elements { get; set; } = new();

    [JsonPropertyName("paging")]
    public LinkedInPaging? Paging { get; set; }
}

public class LinkedInComment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("actor")]
    public string? Actor { get; set; }

    [JsonPropertyName("message")]
    public LinkedInCommentMessage? Message { get; set; }

    [JsonPropertyName("created")]
    public LinkedInAuditStamp? Created { get; set; }

    [JsonPropertyName("lastModified")]
    public LinkedInAuditStamp? LastModified { get; set; }
}

public class LinkedInCommentMessage
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class LinkedInAuditStamp
{
    [JsonPropertyName("time")]
    public long Time { get; set; } // Unix timestamp in milliseconds

    [JsonPropertyName("actor")]
    public string? Actor { get; set; }
}

#endregion

#region Common DTOs

public class LinkedInPaging
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("links")]
    public List<LinkedInPagingLink>? Links { get; set; }
}

public class LinkedInPagingLink
{
    [JsonPropertyName("rel")]
    public string Rel { get; set; } = string.Empty;

    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class LinkedInMediaReference
{
    [JsonPropertyName("original")]
    public string? Original { get; set; }

    [JsonPropertyName("cropped")]
    public string? Cropped { get; set; }
}

public class LinkedInErrorResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("serviceErrorCode")]
    public int? ServiceErrorCode { get; set; }
}

#endregion
