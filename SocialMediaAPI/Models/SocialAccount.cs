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

    // LinkedIn-specific fields for organization/company page support
    [StringLength(256)]
    public string? OrganizationId { get; set; } // LinkedIn organization URN (e.g., urn:li:organization:123456)

    [StringLength(200)]
    public string? OrganizationName { get; set; } // Company/Organization name

    [StringLength(50)]
    public string AccountType { get; set; } = "Personal"; // "Personal" or "Organization"

    public string? Scopes { get; set; } // OAuth scopes granted (comma-separated)

    public string? AdditionalData { get; set; } // JSON field for platform-specific data

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
