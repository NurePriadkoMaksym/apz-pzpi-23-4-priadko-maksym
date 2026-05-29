using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Controllers
{
    [ApiController]
    [Route("api/graph")]
    [Authorize] 
    public class ArticleGraphController : ControllerBase
    {
        private readonly IArticleGraphService _graphService;

        public ArticleGraphController(IArticleGraphService graphService)
        {
            _graphService = graphService;
        }

        [HttpGet("dot")]
        public async Task<IActionResult> GetDotGraph()
        {
            var dot = await _graphService.ExportDotAsync();
            return Content(dot, "text/plain");
        }

        [HttpGet("graphml")]
        public async Task<IActionResult> GetGraphML()
        {
            var xml = await _graphService.ExportGraphMLAsync();
            return Content(xml, "application/xml");
        }
        [HttpGet("png")]
        public async Task<IActionResult> GetGraphPng()
        {
            var pngBytes = await _graphService.ExportPngAsync();
            return File(pngBytes, "image/png");
        }

    }
}
