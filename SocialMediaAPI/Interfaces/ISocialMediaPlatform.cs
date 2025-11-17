using SocialMediaAPI.Models;

namespace SocialMediaAPI.Interfaces;

public interface ISocialMediaPlatform
{
    string PlatformName { get; }
    Task<bool> ValidateConnectionAsync(string accessToken);
    Task<string> PublishPostAsync(string accessToken, string content, List<string>? mediaUrls);
    Task<PostAnalytics> GetPostAnalyticsAsync(string accessToken, string platformPostId);
    Task<List<Comment>> GetCommentsAsync(string accessToken, string platformPostId);
    Task<bool> ReplyToCommentAsync(string accessToken, string commentId, string reply);
    Task<List<DirectMessage>> GetDirectMessagesAsync(string accessToken);
}
