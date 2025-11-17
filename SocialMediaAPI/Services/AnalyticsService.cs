using Microsoft.EntityFrameworkCore;
using SocialMediaAPI.Data;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;
using System.Text.Json;

namespace SocialMediaAPI.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly IEnumerable<ISocialMediaPlatform> _platforms;

    public AnalyticsService(ApplicationDbContext context, IEnumerable<ISocialMediaPlatform> platforms)
    {
        _context = context;
        _platforms = platforms;
    }

    public async Task<AggregatedAnalyticsResponse> GetPostAnalyticsAsync(int postId, int userId)
    {
        var post = await _context.Posts
            .Include(p => p.Analytics)
            .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

        if (post == null)
        {
            throw new InvalidOperationException("Post not found");
        }

        var analytics = post.Analytics.ToList();

        var response = new AggregatedAnalyticsResponse
        {
            TotalLikes = analytics.Sum(a => a.Likes),
            TotalComments = analytics.Sum(a => a.Comments),
            TotalShares = analytics.Sum(a => a.Shares),
            TotalViews = analytics.Sum(a => a.Views),
            TotalClicks = analytics.Sum(a => a.Clicks),
            ByPlatform = analytics.Select(a => new AnalyticsResponse
            {
                Platform = a.Platform,
                Likes = a.Likes,
                Comments = a.Comments,
                Shares = a.Shares,
                Views = a.Views,
                Clicks = a.Clicks,
                FetchedAt = a.FetchedAt
            }).ToList()
        };

        return response;
    }

    public async Task RefreshPostAnalyticsAsync(int postId, int userId)
    {
        var post = await _context.Posts
            .Include(p => p.PostResults)
            .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

        if (post == null)
        {
            throw new InvalidOperationException("Post not found");
        }

        var socialAccounts = await _context.SocialAccounts
            .Where(sa => sa.UserId == userId && sa.IsActive)
            .ToListAsync();

        var successfulResults = post.PostResults.Where(pr => pr.Status == "success").ToList();

        foreach (var result in successfulResults)
        {
            var account = socialAccounts.FirstOrDefault(sa => 
                sa.Platform.Equals(result.Platform, StringComparison.OrdinalIgnoreCase));

            if (account == null || string.IsNullOrEmpty(result.PlatformPostId))
            {
                continue;
            }

            var platform = _platforms.FirstOrDefault(p => 
                p.PlatformName.Equals(result.Platform, StringComparison.OrdinalIgnoreCase));

            if (platform == null)
            {
                continue;
            }

            try
            {
                var analytics = await platform.GetPostAnalyticsAsync(account.AccessToken!, result.PlatformPostId);

                // Update or create analytics record
                var existingAnalytics = await _context.PostAnalytics
                    .FirstOrDefaultAsync(pa => pa.PostId == postId && pa.Platform == result.Platform);

                if (existingAnalytics != null)
                {
                    existingAnalytics.Likes = analytics.Likes;
                    existingAnalytics.Comments = analytics.Comments;
                    existingAnalytics.Shares = analytics.Shares;
                    existingAnalytics.Views = analytics.Views;
                    existingAnalytics.Clicks = analytics.Clicks;
                    existingAnalytics.FetchedAt = DateTime.UtcNow;
                }
                else
                {
                    analytics.PostId = postId;
                    analytics.Platform = result.Platform;
                    analytics.FetchedAt = DateTime.UtcNow;
                    _context.PostAnalytics.Add(analytics);
                }
            }
            catch
            {
                // Ignore errors when fetching analytics
                continue;
            }
        }

        await _context.SaveChangesAsync();
    }
}
