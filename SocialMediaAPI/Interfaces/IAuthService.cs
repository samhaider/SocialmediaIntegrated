using SocialMediaAPI.DTOs;

namespace SocialMediaAPI.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    string GenerateJwtToken(int userId, string email, string? tenantId);
}
