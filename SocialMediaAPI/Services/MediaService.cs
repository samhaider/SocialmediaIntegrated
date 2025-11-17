using SocialMediaAPI.Data;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Services;

public class MediaService : IMediaService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly string _uploadPath;

    public MediaService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _uploadPath = _configuration["MediaStorage:Path"] ?? "uploads";

        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> UploadMediaAsync(int userId, Stream fileStream, string fileName, string contentType)
    {
        var fileExtension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(_uploadPath, uniqueFileName);

        using (var fileStreamOut = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOut);
        }

        var fileInfo = new FileInfo(filePath);
        var mediaType = GetMediaType(contentType);

        var media = new Media
        {
            UserId = userId,
            FileName = fileName,
            Url = $"/media/{uniqueFileName}",
            MediaType = mediaType,
            FileSize = fileInfo.Length,
            MimeType = contentType,
            UploadedAt = DateTime.UtcNow,
            IsProcessed = true
        };

        _context.Media.Add(media);
        await _context.SaveChangesAsync();

        return media.Url;
    }

    public async Task<bool> DeleteMediaAsync(int mediaId, int userId)
    {
        var media = await _context.Media.FindAsync(mediaId);

        if (media == null || media.UserId != userId)
        {
            return false;
        }

        var fileName = Path.GetFileName(media.Url);
        var filePath = Path.Combine(_uploadPath, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        _context.Media.Remove(media);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<string> GetMediaUrlAsync(int mediaId)
    {
        var media = await _context.Media.FindAsync(mediaId);
        return media?.Url ?? throw new InvalidOperationException("Media not found");
    }

    private static string GetMediaType(string contentType)
    {
        if (contentType.StartsWith("image/"))
            return "image";
        if (contentType.StartsWith("video/"))
            return "video";
        if (contentType.Contains("gif"))
            return "gif";

        return "other";
    }
}
