using Microsoft.AspNetCore.Mvc;

namespace PythonWiki.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "PythonWiki API",
            machine = Environment.MachineName,
            pod = Environment.MachineName,
            timeUtc = DateTime.UtcNow
        });
    }
}
