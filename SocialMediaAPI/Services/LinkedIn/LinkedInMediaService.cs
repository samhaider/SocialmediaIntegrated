using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SocialMediaAPI.Configuration;
using SocialMediaAPI.DTOs;

namespace SocialMediaAPI.Services.LinkedIn;

public interface ILinkedInMediaService
{
    Task<string> UploadImageAsync(string accessToken, string ownerUrn, byte[] imageData, string fileName);
    Task<string> UploadVideoAsync(string accessToken, string ownerUrn, byte[] videoData, string fileName);
    Task<LinkedInRegisterUploadResponse> RegisterUploadAsync(string accessToken, LinkedInRegisterUploadRequest request);
    Task UploadMediaToUrlAsync(string uploadUrl, byte[] data);
}

public class LinkedInMediaService : ILinkedInMediaService
{
    private readonly HttpClient _httpClient;
    private readonly LinkedInSettings _settings;
    private readonly ILogger<LinkedInMediaService> _logger;

    // LinkedIn media recipes
    private const string IMAGE_RECIPE = "urn:li:digitalmediaRecipe:feedshare-image";
    private const string VIDEO_RECIPE = "urn:li:digitalmediaRecipe:feedshare-video";

    public LinkedInMediaService(
        HttpClient httpClient,
        IOptions<LinkedInSettings> settings,
        ILogger<LinkedInMediaService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(
        string accessToken,
        string ownerUrn,
        byte[] imageData,
        string fileName)
    {
        try
        {
            _logger.LogInformation("Starting image upload for owner: {Owner}, file: {FileName}",
                ownerUrn, fileName);

            // Step 1: Register upload
            var registerRequest = new LinkedInRegisterUploadRequest
            {
                InitializeUploadRequest = new LinkedInInitializeUploadRequest
                {
                    Owner = ownerUrn,
                    FileSizeBytes = imageData.Length,
                    UploadCausalityDirection = "UPLOAD_NEW_MEDIA",
                    Recipes = new List<string> { IMAGE_RECIPE },
                    FileExtension = Path.GetExtension(fileName).TrimStart('.')
                }
            };

            var registerResponse = await RegisterUploadAsync(accessToken, registerRequest);

            if (registerResponse?.Value?.UploadUrl == null && registerResponse?.Value?.Image == null)
            {
                throw new InvalidOperationException("Failed to get upload URL from register response");
            }

            // Step 2: Upload the image
            var uploadUrl = registerResponse.Value.UploadUrl ?? registerResponse.Value.Image;
            if (!string.IsNullOrEmpty(uploadUrl))
            {
                await UploadMediaToUrlAsync(uploadUrl, imageData);
            }
            else if (registerResponse.Value.UploadInstructions?.Any() == true)
            {
                // Multi-part upload for larger files
                await UploadInChunks(registerResponse.Value.UploadInstructions, imageData);
            }

            // Step 3: Return the asset URN
            var assetUrn = registerResponse.Value.Asset ?? registerResponse.Value.Image;

            if (string.IsNullOrEmpty(assetUrn))
            {
                throw new InvalidOperationException("Failed to get asset URN from upload response");
            }

            _logger.LogInformation("Successfully uploaded image. Asset URN: {AssetUrn}", assetUrn);
            return assetUrn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> UploadVideoAsync(
        string accessToken,
        string ownerUrn,
        byte[] videoData,
        string fileName)
    {
        try
        {
            _logger.LogInformation("Starting video upload for owner: {Owner}, file: {FileName}",
                ownerUrn, fileName);

            // Step 1: Register upload
            var registerRequest = new LinkedInRegisterUploadRequest
            {
                InitializeUploadRequest = new LinkedInInitializeUploadRequest
                {
                    Owner = ownerUrn,
                    FileSizeBytes = videoData.Length,
                    UploadCausalityDirection = "UPLOAD_NEW_MEDIA",
                    Recipes = new List<string> { VIDEO_RECIPE },
                    FileExtension = Path.GetExtension(fileName).TrimStart('.')
                }
            };

            var registerResponse = await RegisterUploadAsync(accessToken, registerRequest);

            if (registerResponse?.Value == null)
            {
                throw new InvalidOperationException("Failed to register video upload");
            }

            // Step 2: Upload the video (usually in chunks for larger files)
            if (registerResponse.Value.UploadInstructions?.Any() == true)
            {
                await UploadInChunks(registerResponse.Value.UploadInstructions, videoData);
            }
            else if (!string.IsNullOrEmpty(registerResponse.Value.UploadUrl))
            {
                await UploadMediaToUrlAsync(registerResponse.Value.UploadUrl, videoData);
            }

            // Step 3: Return the asset URN
            var assetUrn = registerResponse.Value.Asset ?? registerResponse.Value.Video;

            if (string.IsNullOrEmpty(assetUrn))
            {
                throw new InvalidOperationException("Failed to get asset URN from upload response");
            }

            _logger.LogInformation("Successfully uploaded video. Asset URN: {AssetUrn}", assetUrn);
            return assetUrn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video: {FileName}", fileName);
            throw;
        }
    }

    public async Task<LinkedInRegisterUploadResponse> RegisterUploadAsync(
        string accessToken,
        LinkedInRegisterUploadRequest request)
    {
        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{_settings.ApiBaseUrl}/assets?action=initializeUpload");

            httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
            httpRequest.Headers.Add("LinkedIn-Version", _settings.ApiVersion);
            httpRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogDebug("Registering upload with request: {Request}", jsonContent);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to register upload. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"Register upload failed: {response.StatusCode}");
            }

            var registerResponse = JsonSerializer.Deserialize<LinkedInRegisterUploadResponse>(
                responseContent, jsonOptions);

            if (registerResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize register upload response");
            }

            _logger.LogInformation("Successfully registered upload. Asset: {Asset}",
                registerResponse.Value?.Asset);

            return registerResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering upload");
            throw;
        }
    }

    public async Task UploadMediaToUrlAsync(string uploadUrl, byte[] data)
    {
        try
        {
            _logger.LogInformation("Uploading {Size} bytes to URL", data.Length);

            using var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.PutAsync(uploadUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to upload media. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"Media upload failed: {response.StatusCode}");
            }

            _logger.LogInformation("Successfully uploaded media");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media to URL");
            throw;
        }
    }

    private async Task UploadInChunks(List<LinkedInUploadInstruction> instructions, byte[] data)
    {
        _logger.LogInformation("Uploading media in {Count} chunks", instructions.Count);

        foreach (var instruction in instructions)
        {
            var chunkSize = (int)(instruction.LastByteIndex - instruction.FirstByteIndex + 1);
            var chunk = new byte[chunkSize];
            Array.Copy(data, instruction.FirstByteIndex, chunk, 0, chunkSize);

            _logger.LogDebug("Uploading chunk: {FirstByte} - {LastByte}",
                instruction.FirstByteIndex, instruction.LastByteIndex);

            await UploadMediaToUrlAsync(instruction.UploadUrl, chunk);
        }

        _logger.LogInformation("Successfully uploaded all chunks");
    }
}
