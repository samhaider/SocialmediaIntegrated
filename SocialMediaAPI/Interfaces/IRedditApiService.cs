using SocialMediaAPI.DTOs;

namespace SocialMediaAPI.Interfaces;

/// <summary>
/// Service for Reddit API operations (posting, commenting, media upload)
/// </summary>
public interface IRedditApiService
{
    /// <summary>
    /// Submit a new post to Reddit
    /// </summary>
    /// <param name="accessToken">Valid OAuth access token with 'submit' scope</param>
    /// <param name="request">Post submission request</param>
    /// <returns>Submission response with post ID and URL</returns>
    Task<RedditSubmitResponse> SubmitPostAsync(string accessToken, RedditPostRequest request);

    /// <summary>
    /// Submit a post with media (image/video)
    /// </summary>
    /// <param name="accessToken">Valid OAuth access token with 'submit' scope</param>
    /// <param name="request">Post submission request</param>
    /// <param name="mediaFilePath">Path to media file to upload</param>
    /// <returns>Submission response with post ID and URL</returns>
    Task<RedditSubmitResponse> SubmitPostWithMediaAsync(string accessToken, RedditPostRequest request, string mediaFilePath);

    /// <summary>
    /// Create a comment on a post or reply to a comment
    /// </summary>
    /// <param name="accessToken">Valid OAuth access token with 'submit' scope</param>
    /// <param name="request">Comment request</param>
    /// <returns>Comment response with comment ID</returns>
    Task<RedditCommentResponse> CreateCommentAsync(string accessToken, RedditCommentRequest request);

    /// <summary>
    /// Edit a text post or comment
    /// </summary>
    /// <param name="accessToken">Valid OAuth access token with 'edit' scope</param>
    /// <param name="thingId">Full name of the thing to edit (e.g., t3_xxx for post, t1_xxx for comment)</param>
    /// <param name="newText">New markdown text content</param>
    /// <returns>True if edit successful</returns>
    Task<bool> EditTextAsync(string accessToken, string thingId, string newText);

    /// <summary>
    /// Delete a post or comment
    /// </summary>
    /// <param name="accessToken">Valid OAuth access token with 'edit' scope</param>
    /// <param name="thingId">Full name of the thing to delete</param>
    /// <returns>True if deletion successful</returns>
    Task<bool> DeleteAsync(string accessToken, string thingId);

    /// <summary>
    /// Upload media asset to Reddit
    /// </summary>
    /// <param name="accessToken">Valid OAuth access token</param>
    /// <param name="filePath">Path to media file</param>
    /// <param name="mimeType">MIME type of media file</param>
    /// <returns>Media asset response with upload URL and asset ID</returns>
    Task<RedditMediaAssetResponse> UploadMediaAssetAsync(string accessToken, string filePath, string mimeType);

    /// <summary>
    /// Get subreddit submit requirements
    /// </summary>
    /// <param name="accessToken">Valid OAuth access token</param>
    /// <param name="subreddit">Subreddit name (without /r/)</param>
    /// <returns>Submit requirements data</returns>
    Task<Dictionary<string, object>> GetSubmitRequirementsAsync(string accessToken, string subreddit);
}
