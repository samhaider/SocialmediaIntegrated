using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaAPI.Models;

public class Post
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? MediaUrls { get; set; } // JSON array of media URLs

    public string? Platforms { get; set; } // JSON array of platform names

    public string Status { get; set; } = "draft"; // draft, scheduled, published, failed

    public DateTime? ScheduledAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<PostResult> PostResults { get; set; } = new List<PostResult>();
    public virtual ICollection<PostAnalytics> Analytics { get; set; } = new List<PostAnalytics>();
}
