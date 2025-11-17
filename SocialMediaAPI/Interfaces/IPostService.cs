using SocialMediaAPI.DTOs;

namespace SocialMediaAPI.Interfaces;

public interface IPostService
{
    Task<PostResponse> CreatePostAsync(int userId, CreatePostRequest request);
    Task<PostResponse> GetPostAsync(int postId, int userId);
    Task<List<PostResponse>> GetUserPostsAsync(int userId);
    Task<bool> PublishPostAsync(int postId, int userId);
    Task<bool> DeletePostAsync(int postId, int userId);
}
