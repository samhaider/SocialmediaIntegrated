using System.Text.Json.Serialization;

namespace SocialMediaAPI.DTOs.Instagram;

/// <summary>
/// Response containing Facebook Pages connected to user
/// </summary>
public class FacebookPagesResponse
{
    [JsonPropertyName("data")]
    public List<FacebookPage> Data { get; set; } = new();

    [JsonPropertyName("paging")]
    public PagingInfo? Paging { get; set; }
}

public class FacebookPage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("instagram_business_account")]
    public InstagramBusinessAccount? InstagramBusinessAccount { get; set; }
}

/// <summary>
/// Instagram Business Account information
/// </summary>
public class InstagramBusinessAccount
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("profile_picture_url")]
    public string? ProfilePictureUrl { get; set; }

    [JsonPropertyName("followers_count")]
    public int? FollowersCount { get; set; }

    [JsonPropertyName("follows_count")]
    public int? FollowsCount { get; set; }

    [JsonPropertyName("media_count")]
    public int? MediaCount { get; set; }
}

public class PagingInfo
{
    [JsonPropertyName("cursors")]
    public CursorsInfo? Cursors { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

public class CursorsInfo
{
    [JsonPropertyName("before")]
    public string? Before { get; set; }

    [JsonPropertyName("after")]
    public string? After { get; set; }
}
