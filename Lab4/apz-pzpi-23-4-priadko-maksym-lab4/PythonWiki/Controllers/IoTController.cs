using Microsoft.AspNetCore.Mvc;
using PythonWiki.Dtos.IoT;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Controllers
{
    [ApiController]
    [Route("api/iot")]
    public class IoTController : ControllerBase
    {
        private readonly IIoTService _service;

        public IoTController(IIoTService service)
        {
            _service = service;
        }

        [HttpGet("config/{deviceId}")]
        public IActionResult GetConfig(string deviceId)
        {
            var cfg = _service.GetConfig(deviceId);
            return Ok(cfg);
        }

        [HttpPost("config/{deviceId}")]
        public IActionResult UpdateConfig(string deviceId, [FromBody] DeviceConfigDto dto)
        {
            var cfg = _service.UpdateConfig(deviceId, dto);
            return Ok(cfg);
        }

        [HttpPost("log")]
        public IActionResult LogTelemetry([FromBody] TelemetryDto dto)
        {
            _service.LogTelemetry(dto);
            return Ok(new { success = true });
        }

        [HttpGet("log")]
        public IActionResult GetLogs()
        {
            return Ok(_service.GetLogs());
        }
    }
}
