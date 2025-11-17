using System.ComponentModel.DataAnnotations;

namespace SocialMediaAPI.Models;

public class Comment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PostId { get; set; }

    [Required]
    [StringLength(50)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    public string PlatformCommentId { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [StringLength(200)]
    public string? AuthorName { get; set; }

    public string? AuthorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsReplied { get; set; } = false;

    public string? Reply { get; set; }

    public DateTime? RepliedAt { get; set; }
}
