using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Interfaces;
using System.Security.Claims;

namespace SocialMediaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SocialAccountsController : ControllerBase
{
    private readonly ISocialAccountService _socialAccountService;

    public SocialAccountsController(ISocialAccountService socialAccountService)
    {
        _socialAccountService = socialAccountService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    [HttpPost]
    public async Task<ActionResult<SocialAccountResponse>> ConnectAccount([FromBody] ConnectSocialAccountRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await _socialAccountService.ConnectAccountAsync(userId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<SocialAccountResponse>>> GetUserAccounts()
    {
        try
        {
            var userId = GetUserId();
            var response = await _socialAccountService.GetUserAccountsAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{accountId}")]
    public async Task<ActionResult> DisconnectAccount(int accountId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _socialAccountService.DisconnectAccountAsync(accountId, userId);
            return success ? Ok(new { message = "Account disconnected successfully" }) 
                           : NotFound(new { message = "Account not found" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
