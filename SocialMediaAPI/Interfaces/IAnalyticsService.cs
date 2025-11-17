using SocialMediaAPI.DTOs;

namespace SocialMediaAPI.Interfaces;

public interface IAnalyticsService
{
    Task<AggregatedAnalyticsResponse> GetPostAnalyticsAsync(int postId, int userId);
    Task RefreshPostAnalyticsAsync(int postId, int userId);
}
