using Microsoft.EntityFrameworkCore;
using SocialMediaAPI.Data;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;
using SocialMediaAPI.Services.LinkedIn;

namespace SocialMediaAPI.Services;

public class SocialAccountService : ISocialAccountService
{
    private readonly ApplicationDbContext _context;
    private readonly IEnumerable<ISocialMediaPlatform> _platforms;
    private readonly ILinkedInOAuthService _linkedInOAuthService;
    private readonly ILogger<SocialAccountService> _logger;

    public SocialAccountService(
        ApplicationDbContext context,
        IEnumerable<ISocialMediaPlatform> platforms,
        ILinkedInOAuthService linkedInOAuthService,
        ILogger<SocialAccountService> logger)
    {
        _context = context;
        _platforms = platforms;
        _linkedInOAuthService = linkedInOAuthService;
        _logger = logger;
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

        // Get account ID from platform
        string accountId;
        string? accountName = null;

        // For LinkedIn, fetch user info and organization details
        if (request.Platform.Equals("LinkedIn", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var userInfo = await _linkedInOAuthService.GetUserInfoAsync(request.AccessToken);
                accountId = userInfo.Sub; // LinkedIn user ID
                accountName = userInfo.Name ?? userInfo.Email;

                _logger.LogInformation("Retrieved LinkedIn user info. Account ID: {AccountId}, Name: {Name}",
                    accountId, accountName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve LinkedIn user info");
                accountId = Guid.NewGuid().ToString();
            }
        }
        else
        {
            accountId = Guid.NewGuid().ToString(); // In real implementation, fetch from platform
        }

        // Check if account already exists
        var existingAccount = await _context.SocialAccounts
            .FirstOrDefaultAsync(sa => sa.UserId == userId &&
                                      sa.Platform == request.Platform &&
                                      sa.AccountId == accountId);

        if (existingAccount != null)
        {
            // Update existing account
            existingAccount.AccessToken = request.AccessToken;
            existingAccount.RefreshToken = request.RefreshToken;
            existingAccount.TokenExpiresAt = request.TokenExpiresAt;
            existingAccount.AccountName = accountName ?? existingAccount.AccountName;
            existingAccount.OrganizationId = request.OrganizationId;
            existingAccount.OrganizationName = request.OrganizationName;
            existingAccount.AccountType = request.AccountType;
            existingAccount.Scopes = request.Scopes;
            existingAccount.IsActive = true;
            existingAccount.LastSyncedAt = DateTime.UtcNow;

            _logger.LogInformation("Updated existing social account. ID: {AccountId}, Platform: {Platform}",
                existingAccount.Id, request.Platform);
        }
        else
        {
            // Create new account
            var account = new SocialAccount
            {
                UserId = userId,
                Platform = request.Platform,
                AccountId = accountId,
                AccountName = accountName,
                AccessToken = request.AccessToken,
                RefreshToken = request.RefreshToken,
                TokenExpiresAt = request.TokenExpiresAt,
                OrganizationId = request.OrganizationId,
                OrganizationName = request.OrganizationName,
                AccountType = request.AccountType,
                Scopes = request.Scopes,
                IsActive = true,
                ConnectedAt = DateTime.UtcNow
            };

            _context.SocialAccounts.Add(account);

            _logger.LogInformation("Created new social account. Platform: {Platform}, AccountType: {AccountType}",
                request.Platform, request.AccountType);
        }

        await _context.SaveChangesAsync();

        var updatedAccount = await _context.SocialAccounts
            .FirstAsync(sa => sa.UserId == userId &&
                             sa.Platform == request.Platform &&
                             sa.AccountId == accountId);

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
            ConnectedAt = account.ConnectedAt,
            OrganizationId = account.OrganizationId,
            OrganizationName = account.OrganizationName,
            AccountType = account.AccountType,
            Scopes = account.Scopes
        };
    }
}
