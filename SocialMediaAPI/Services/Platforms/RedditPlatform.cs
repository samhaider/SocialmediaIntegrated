using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Services.Platforms;

public class RedditPlatform : ISocialMediaPlatform
{
    public string PlatformName => "Reddit";

    public Task<bool> ValidateConnectionAsync(string accessToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(accessToken));
    }

    public Task<string> PublishPostAsync(string accessToken, string content, List<string>? mediaUrls)
    {
        var postId = $"rd_{Guid.NewGuid().ToString().Substring(0, 8)}";
        return Task.FromResult(postId);
    }

    public Task<PostAnalytics> GetPostAnalyticsAsync(string accessToken, string platformPostId)
    {
        return Task.FromResult(new PostAnalytics
        {
            Likes = new Random().Next(10, 100),
            Comments = new Random().Next(5, 50),
            Shares = new Random().Next(0, 30),
            Views = new Random().Next(100, 1000),
            Clicks = new Random().Next(10, 200)
        });
    }

    public Task<List<Comment>> GetCommentsAsync(string accessToken, string platformPostId)
    {
        return Task.FromResult(new List<Comment>());
    }

    public Task<bool> ReplyToCommentAsync(string accessToken, string commentId, string reply)
    {
        return Task.FromResult(true);
    }

    public Task<List<DirectMessage>> GetDirectMessagesAsync(string accessToken)
    {
        return Task.FromResult(new List<DirectMessage>());
    }
}
