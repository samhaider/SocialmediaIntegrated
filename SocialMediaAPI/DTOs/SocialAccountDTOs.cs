namespace SocialMediaAPI.DTOs;

public class ConnectSocialAccountRequest
{
    public string Platform { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }

    // LinkedIn-specific fields
    public string? OrganizationId { get; set; } // LinkedIn organization URN
    public string? OrganizationName { get; set; }
    public string AccountType { get; set; } = "Personal"; // "Personal" or "Organization"
    public string? Scopes { get; set; }
}

public class SocialAccountResponse
{
    public int Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public bool IsActive { get; set; }
    public DateTime ConnectedAt { get; set; }

    // LinkedIn-specific fields
    public string? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string AccountType { get; set; } = "Personal";
    public string? Scopes { get; set; }
}
