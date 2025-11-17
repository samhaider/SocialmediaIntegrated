using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaAPI.Models;

public class PostResult
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PostId { get; set; }

    [Required]
    [StringLength(50)]
    public string Platform { get; set; } = string.Empty;

    public string? PlatformPostId { get; set; }

    public string Status { get; set; } = "pending"; // pending, success, failed

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("PostId")]
    public virtual Post Post { get; set; } = null!;
}
