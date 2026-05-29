using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PythonWiki.Services.Interfaces;
using System.Security.Claims;

namespace PythonWiki.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> WhoAmI()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var profile = await _userService.GetMyProfileAsync(userId);
        return Ok(profile);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserProfile(int id)
    {
        try
        {
            var profile = await _userService.GetUserProfileByIdAsync(id);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("debug")]
    public IActionResult Debug()
    {
        return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        try
        {
            await _userService.DeleteUserAsync(userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

}
