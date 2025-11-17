namespace SocialMediaAPI.DTOs;

public class ConnectSocialAccountRequest
{
    public string Platform { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}

public class SocialAccountResponse
{
    public int Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public bool IsActive { get; set; }
    public DateTime ConnectedAt { get; set; }
}
