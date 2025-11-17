using SocialMediaAPI.DTOs;

namespace SocialMediaAPI.Interfaces;

public interface ISocialAccountService
{
    Task<SocialAccountResponse> ConnectAccountAsync(int userId, ConnectSocialAccountRequest request);
    Task<List<SocialAccountResponse>> GetUserAccountsAsync(int userId);
    Task<bool> DisconnectAccountAsync(int accountId, int userId);
}
