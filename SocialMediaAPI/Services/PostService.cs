using Microsoft.EntityFrameworkCore;
using SocialMediaAPI.Data;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;
using System.Text.Json;

namespace SocialMediaAPI.Services;

public class PostService : IPostService
{
    private readonly ApplicationDbContext _context;
    private readonly IEnumerable<ISocialMediaPlatform> _platforms;

    public PostService(ApplicationDbContext context, IEnumerable<ISocialMediaPlatform> platforms)
    {
        _context = context;
        _platforms = platforms;
    }

    public async Task<PostResponse> CreatePostAsync(int userId, CreatePostRequest request)
    {
        var post = new Post
        {
            UserId = userId,
            Content = request.Content,
            MediaUrls = request.MediaUrls != null ? JsonSerializer.Serialize(request.MediaUrls) : null,
            Platforms = JsonSerializer.Serialize(request.Platforms),
            Status = request.ScheduledAt.HasValue ? "scheduled" : "draft",
            ScheduledAt = request.ScheduledAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return MapToResponse(post);
    }

    public async Task<PostResponse> GetPostAsync(int postId, int userId)
    {
        var post = await _context.Posts
            .Include(p => p.PostResults)
            .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

        if (post == null)
        {
            throw new InvalidOperationException("Post not found");
        }

        return MapToResponse(post);
    }

    public async Task<List<PostResponse>> GetUserPostsAsync(int userId)
    {
        var posts = await _context.Posts
            .Where(p => p.UserId == userId)
            .Include(p => p.PostResults)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return posts.Select(MapToResponse).ToList();
    }

    public async Task<bool> PublishPostAsync(int postId, int userId)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

        if (post == null)
        {
            throw new InvalidOperationException("Post not found");
        }

        var platforms = !string.IsNullOrEmpty(post.Platforms) 
            ? JsonSerializer.Deserialize<List<string>>(post.Platforms) ?? new List<string>()
            : new List<string>();
        var mediaUrls = !string.IsNullOrEmpty(post.MediaUrls) 
            ? JsonSerializer.Deserialize<List<string>>(post.MediaUrls) 
            : null;

        // Get social accounts for the user
        var socialAccounts = await _context.SocialAccounts
            .Where(sa => sa.UserId == userId && sa.IsActive)
            .ToListAsync();

        bool anySuccess = false;

        foreach (var platformName in platforms)
        {
            var account = socialAccounts.FirstOrDefault(sa => 
                sa.Platform.Equals(platformName, StringComparison.OrdinalIgnoreCase));

            if (account == null)
            {
                // Create failed result for missing account
                _context.PostResults.Add(new PostResult
                {
                    PostId = post.Id,
                    Platform = platformName,
                    Status = "failed",
                    ErrorMessage = "Social account not connected",
                    CreatedAt = DateTime.UtcNow
                });
                continue;
            }

            var platform = _platforms.FirstOrDefault(p => 
                p.PlatformName.Equals(platformName, StringComparison.OrdinalIgnoreCase));

            if (platform == null)
            {
                _context.PostResults.Add(new PostResult
                {
                    PostId = post.Id,
                    Platform = platformName,
                    Status = "failed",
                    ErrorMessage = "Platform not supported",
                    CreatedAt = DateTime.UtcNow
                });
                continue;
            }

            try
            {
                var platformPostId = await platform.PublishPostAsync(
                    account.AccessToken!, post.Content, mediaUrls);

                _context.PostResults.Add(new PostResult
                {
                    PostId = post.Id,
                    Platform = platformName,
                    PlatformPostId = platformPostId,
                    Status = "success",
                    CreatedAt = DateTime.UtcNow
                });

                anySuccess = true;
            }
            catch (Exception ex)
            {
                _context.PostResults.Add(new PostResult
                {
                    PostId = post.Id,
                    Platform = platformName,
                    Status = "failed",
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        post.Status = anySuccess ? "published" : "failed";
        post.PublishedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return anySuccess;
    }

    public async Task<bool> DeletePostAsync(int postId, int userId)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

        if (post == null)
        {
            return false;
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return true;
    }

    private static PostResponse MapToResponse(Post post)
    {
        return new PostResponse
        {
            Id = post.Id,
            Content = post.Content,
            MediaUrls = !string.IsNullOrEmpty(post.MediaUrls) 
                ? JsonSerializer.Deserialize<List<string>>(post.MediaUrls) 
                : null,
            Platforms = !string.IsNullOrEmpty(post.Platforms) 
                ? JsonSerializer.Deserialize<List<string>>(post.Platforms) 
                : null,
            Status = post.Status,
            ScheduledAt = post.ScheduledAt,
            PublishedAt = post.PublishedAt,
            CreatedAt = post.CreatedAt,
            Results = post.PostResults?.Select(pr => new PostResultResponse
            {
                Platform = pr.Platform,
                PlatformPostId = pr.PlatformPostId,
                Status = pr.Status,
                ErrorMessage = pr.ErrorMessage
            }).ToList()
        };
    }
}
