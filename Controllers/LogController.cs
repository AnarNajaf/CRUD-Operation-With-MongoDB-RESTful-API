using iTarlaMapBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iTarlaMapBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LogController : ControllerBase
    {
        private readonly LogService _logService;

        public LogController(LogService logService)
        {
            _logService = logService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var logs = await _logService.GetAllLogsAsync(100);
            return Ok(logs);
        }

        [HttpGet("motor/{motorId}")]
        public async Task<IActionResult> GetByMotor(string motorId)
        {
            if (!Guid.TryParse(motorId, out var mId)) return BadRequest("Invalid id.");
            var logs = await _logService.GetLogsAsync(mId, 50);
            return Ok(logs);
        }
    }
}