using Microsoft.EntityFrameworkCore;
using SocialMediaAPI.Data;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Services;

public class SocialAccountService : ISocialAccountService
{
    private readonly ApplicationDbContext _context;
    private readonly IEnumerable<ISocialMediaPlatform> _platforms;

    public SocialAccountService(ApplicationDbContext context, IEnumerable<ISocialMediaPlatform> platforms)
    {
        _context = context;
        _platforms = platforms;
    }

    public async Task<SocialAccountResponse> ConnectAccountAsync(int userId, ConnectSocialAccountRequest request)
    {
        // Validate the connection with the platform
        var platform = _platforms.FirstOrDefault(p => 
            p.PlatformName.Equals(request.Platform, StringComparison.OrdinalIgnoreCase));

        if (platform == null)
        {
            throw new InvalidOperationException("Platform not supported");
        }

        var isValid = await platform.ValidateConnectionAsync(request.AccessToken);
        if (!isValid)
        {
            throw new InvalidOperationException("Invalid access token");
        }

        // Check if account already exists
        var existingAccount = await _context.SocialAccounts
            .FirstOrDefaultAsync(sa => sa.UserId == userId && sa.Platform == request.Platform);

        if (existingAccount != null)
        {
            // Update existing account
            existingAccount.AccessToken = request.AccessToken;
            existingAccount.RefreshToken = request.RefreshToken;
            existingAccount.TokenExpiresAt = request.TokenExpiresAt;
            existingAccount.IsActive = true;
            existingAccount.LastSyncedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new account
            var account = new SocialAccount
            {
                UserId = userId,
                Platform = request.Platform,
                AccountId = Guid.NewGuid().ToString(), // In real implementation, fetch from platform
                AccessToken = request.AccessToken,
                RefreshToken = request.RefreshToken,
                TokenExpiresAt = request.TokenExpiresAt,
                IsActive = true,
                ConnectedAt = DateTime.UtcNow
            };

            _context.SocialAccounts.Add(account);
        }

        await _context.SaveChangesAsync();

        var updatedAccount = await _context.SocialAccounts
            .FirstAsync(sa => sa.UserId == userId && sa.Platform == request.Platform);

        return MapToResponse(updatedAccount);
    }

    public async Task<List<SocialAccountResponse>> GetUserAccountsAsync(int userId)
    {
        var accounts = await _context.SocialAccounts
            .Where(sa => sa.UserId == userId)
            .ToListAsync();

        return accounts.Select(MapToResponse).ToList();
    }

    public async Task<bool> DisconnectAccountAsync(int accountId, int userId)
    {
        var account = await _context.SocialAccounts
            .FirstOrDefaultAsync(sa => sa.Id == accountId && sa.UserId == userId);

        if (account == null)
        {
            return false;
        }

        account.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }

    private static SocialAccountResponse MapToResponse(SocialAccount account)
    {
        return new SocialAccountResponse
        {
            Id = account.Id,
            Platform = account.Platform,
            AccountId = account.AccountId,
            AccountName = account.AccountName,
            IsActive = account.IsActive,
            ConnectedAt = account.ConnectedAt
        };
    }
}
