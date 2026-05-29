using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PythonWiki.Services.Interfaces;
using System.Security.Claims;

namespace PythonWiki.Controllers;

[ApiController]
[Route("api/xp")]
[Authorize]
public class XpController : ControllerBase
{
    private readonly IXpService _xpService;

    public XpController(IXpService xpService)
    {
        _xpService = xpService;
    }

    [HttpPost("complete/{articleId:int}")]
    public async Task<IActionResult> CompleteArticle(int articleId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var xp = await _xpService.CompleteArticleAsync(userId, articleId);
        var level = _xpService.CalculateLevel(xp);

        return Ok(new
        {
            message = "Article completed",
            xp,
            level
        });
    }
}
