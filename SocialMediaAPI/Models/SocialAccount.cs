using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaAPI.Models;

public class SocialAccount
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Platform { get; set; } = string.Empty; // Facebook, Instagram, Twitter, etc.

    [Required]
    [StringLength(256)]
    public string AccountId { get; set; } = string.Empty;

    [StringLength(200)]
    public string? AccountName { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? TokenExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastSyncedAt { get; set; }

    // Facebook Page-specific fields
    /// <summary>
    /// Facebook Page ID (for page accounts). Used when posting to company/business pages.
    /// </summary>
    [StringLength(256)]
    public string? PageId { get; set; }

    /// <summary>
    /// Page-specific access token (for Facebook pages). This is different from user access token.
    /// Page tokens can be long-lived and don't expire unless the user changes their password.
    /// </summary>
    public string? PageAccessToken { get; set; }

    /// <summary>
    /// Display name of the Facebook Page (if applicable).
    /// </summary>
    [StringLength(200)]
    public string? PageName { get; set; }

    /// <summary>
    /// Indicates if the account represents a Facebook Page (true) or personal profile (false).
    /// </summary>
    public bool IsPageAccount { get; set; } = false;

    /// <summary>
    /// Stores platform-specific metadata as JSON (e.g., permissions, page category, followers count).
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
