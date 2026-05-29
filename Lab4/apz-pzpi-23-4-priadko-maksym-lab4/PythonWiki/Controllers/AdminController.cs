using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PythonWiki.Dtos;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IRoleManagementService _roleService;
    private readonly PythonWikiDbContext _db;

    public AdminController(IRoleManagementService roleService, PythonWikiDbContext db)
    {
        _roleService = roleService;
        _db = db;
    }
    [HttpGet]
    [HttpPost("change-role")]
    public async Task<IActionResult> ChangeUserRole([FromBody] ChangeUserRoleRequest request)
    {
        try
        {
            await _roleService.ChangeUserRoleAsync(request);
            return Ok(new { message = "Role updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(roles);
    }
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _db.Users
            .Include(u => u.Role)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.XP,
                Role = u.Role.Name
            })
            .ToListAsync();

        return Ok(users);
    }
}
