namespace SocialMediaAPI.DTOs;

public class CreatePostRequest
{
    public string Content { get; set; } = string.Empty;
    public List<string>? MediaUrls { get; set; }
    public List<string> Platforms { get; set; } = new();
    public DateTime? ScheduledAt { get; set; }
}

public class PostResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<string>? MediaUrls { get; set; }
    public List<string>? Platforms { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PostResultResponse>? Results { get; set; }
}

public class PostResultResponse
{
    public string Platform { get; set; } = string.Empty;
    public string? PlatformPostId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
