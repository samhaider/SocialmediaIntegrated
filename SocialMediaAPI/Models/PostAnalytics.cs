using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaAPI.Models;

public class PostAnalytics
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PostId { get; set; }

    [Required]
    [StringLength(50)]
    public string Platform { get; set; } = string.Empty;

    public int Likes { get; set; } = 0;

    public int Comments { get; set; } = 0;

    public int Shares { get; set; } = 0;

    public int Views { get; set; } = 0;

    public int Clicks { get; set; } = 0;

    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("PostId")]
    public virtual Post Post { get; set; } = null!;
}
