namespace SocialMediaAPI.Interfaces;

public interface IMediaService
{
    Task<string> UploadMediaAsync(int userId, Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteMediaAsync(int mediaId, int userId);
    Task<string> GetMediaUrlAsync(int mediaId);
}
