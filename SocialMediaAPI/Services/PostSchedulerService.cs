using Microsoft.EntityFrameworkCore;
using SocialMediaAPI.Data;
using SocialMediaAPI.Interfaces;

namespace SocialMediaAPI.Services;

public class PostSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PostSchedulerService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public PostSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<PostSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Post Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledPosts();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled posts");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Post Scheduler Service stopped");
    }

    private async Task ProcessScheduledPosts()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var postService = scope.ServiceProvider.GetRequiredService<IPostService>();

        var now = DateTime.UtcNow;

        // Get all scheduled posts that are due
        var duePosts = await context.Posts
            .Where(p => p.Status == "scheduled" && p.ScheduledAt <= now)
            .ToListAsync();

        _logger.LogInformation($"Found {duePosts.Count} scheduled posts to publish");

        foreach (var post in duePosts)
        {
            try
            {
                _logger.LogInformation($"Publishing scheduled post {post.Id}");
                await postService.PublishPostAsync(post.Id, post.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing scheduled post {post.Id}");
                post.Status = "failed";
                await context.SaveChangesAsync();
            }
        }
    }
}
