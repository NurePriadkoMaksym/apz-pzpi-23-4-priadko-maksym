using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PythonWiki.Dtos.Articles;
using PythonWiki.Services.Interfaces;
using System.Security.Claims;

namespace PythonWiki.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticleController : ControllerBase
{
    private readonly IArticleService _articleService;

    public ArticleController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [Authorize(Roles = "Editor,Creator,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateArticleRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int authorId))
            return Unauthorized();

        var created = await _articleService.CreateArticleAsync(authorId, request);
        return Ok(created);
    }

    [Authorize(Roles = "Editor,Creator,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _articleService.DeleteArticleAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
    [Authorize(Roles = "Editor,Creator,Admin")]
    
    [HttpPost("link")]
    public async Task<IActionResult> AddLink([FromBody] CreateArticleLinkRequest request)
    {
        try
        {
            await _articleService.AddLinkAsync(request.FromArticleId, request.ToArticleId);
            return Ok(new { message = "Link created" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Roles = "Editor,Creator,Admin")]
    [HttpDelete("link")]
    public async Task<IActionResult> RemoveLink([FromBody] DeleteArticleLinkRequest request)
    {
        try
        {
            await _articleService.RemoveLinkAsync(request.FromArticleId, request.ToArticleId);
            return Ok(new { message = "Link removed" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Roles = "Editor,Creator,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateArticleRequest request)
    {
        try
        {
            var editorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _articleService.UpdateArticleAsync(id, editorId, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
    [Authorize]
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableArticles()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var articles = await _articleService.GetAvailableArticlesAsync(userId);
        return Ok(articles);
    }
    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetArticle(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var article = await _articleService.GetArticleByIdAsync(userId, id);
        return Ok(article);
    }
    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        var results = await _articleService.SearchAsync(keyword);

        if (results.Count == 0)
            return NotFound("No articles found.");

        // Return the first match for IoT simplicity
        return Ok(results.First());
    }
}
