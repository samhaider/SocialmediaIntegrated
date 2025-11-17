using Microsoft.EntityFrameworkCore;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<SocialAccount> SocialAccounts { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostResult> PostResults { get; set; }
    public DbSet<PostAnalytics> PostAnalytics { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<DirectMessage> DirectMessages { get; set; }
    public DbSet<Media> Media { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // Configure SocialAccount
        modelBuilder.Entity<SocialAccount>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Platform, e.AccountId }).IsUnique();
        });

        // Configure Post
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledAt);
        });

        // Configure Comment
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasIndex(e => e.PostId);
            entity.HasIndex(e => new { e.Platform, e.PlatformCommentId }).IsUnique();
        });

        // Configure DirectMessage
        modelBuilder.Entity<DirectMessage>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.Platform, e.PlatformMessageId }).IsUnique();
        });

        // Configure Media
        modelBuilder.Entity<Media>(entity =>
        {
            entity.HasIndex(e => e.UserId);
        });
    }
}
