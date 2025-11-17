using System.ComponentModel.DataAnnotations;

namespace SocialMediaAPI.Models;

public class DirectMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    public string PlatformMessageId { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [StringLength(200)]
    public string? SenderName { get; set; }

    public string? SenderId { get; set; }

    public bool IsIncoming { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }
}
