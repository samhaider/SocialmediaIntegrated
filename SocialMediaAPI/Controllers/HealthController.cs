using Microsoft.AspNetCore.Mvc;
using SocialMediaAPI.Interfaces;

namespace SocialMediaAPI.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    private readonly IEnumerable<ISocialMediaPlatform> _platforms;

    public HealthController(IEnumerable<ISocialMediaPlatform> platforms)
    {
        _platforms = platforms;
    }

    [HttpGet("health")]
    public ActionResult<object> GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpGet("platforms")]
    public ActionResult<object> GetPlatforms()
    {
        var platformList = _platforms.Select(p => p.PlatformName).OrderBy(p => p).ToList();
        return Ok(new
        {
            count = platformList.Count,
            platforms = platformList
        });
    }
}
