using SocialMediaAPI.DTOs;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Interfaces;

/// <summary>
/// Service interface for Pinterest API v5 integration
/// </summary>
public interface IPinterestService
{
    /// <summary>
    /// Validates the Pinterest access token by fetching user account info
    /// </summary>
    Task<bool> ValidateAccessTokenAsync(string accessToken);

    /// <summary>
    /// Refreshes an expired access token using a refresh token
    /// </summary>
    Task<PinterestTokenResponse?> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Gets the authenticated user's account information
    /// </summary>
    Task<PinterestUserResponse?> GetUserAccountAsync(string accessToken);

    /// <summary>
    /// Lists all boards for the authenticated user
    /// </summary>
    Task<PinterestBoardsResponse?> GetBoardsAsync(string accessToken, string? pageSize = null, string? bookmark = null);

    /// <summary>
    /// Creates a new pin on a specified board
    /// </summary>
    Task<PinterestPin?> CreatePinAsync(string accessToken, PinterestCreatePinRequest request);

    /// <summary>
    /// Gets details of a specific pin
    /// </summary>
    Task<PinterestPin?> GetPinAsync(string accessToken, string pinId);

    /// <summary>
    /// Lists all pins for the authenticated user
    /// </summary>
    Task<PinterestPinsResponse?> GetUserPinsAsync(string accessToken, string? pageSize = null, string? bookmark = null);

    /// <summary>
    /// Lists pins on a specific board
    /// </summary>
    Task<PinterestPinsResponse?> GetBoardPinsAsync(string accessToken, string boardId, string? pageSize = null, string? bookmark = null);

    /// <summary>
    /// Gets analytics for a specific pin
    /// </summary>
    Task<PinterestPinAnalytics?> GetPinAnalyticsAsync(string accessToken, string pinId, string startDate, string endDate);

    /// <summary>
    /// Deletes a pin
    /// </summary>
    Task<bool> DeletePinAsync(string accessToken, string pinId);
}
