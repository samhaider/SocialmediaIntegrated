using System.ComponentModel.DataAnnotations;

namespace SocialMediaAPI.Models;

public class Media
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(256)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string Url { get; set; } = string.Empty;

    [StringLength(50)]
    public string MediaType { get; set; } = string.Empty; // image, video, gif

    public long FileSize { get; set; }

    [StringLength(100)]
    public string? MimeType { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public bool IsProcessed { get; set; } = false;
}
