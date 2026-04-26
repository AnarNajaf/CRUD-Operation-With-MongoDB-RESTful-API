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

        // GET /api/log?skip=0&limit=50
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int limit = 50)
        {
            var logs  = await _logService.GetAllLogsAsync(skip, limit);
            var total = await _logService.CountAllLogsAsync();
            return Ok(new { total, logs });
        }

        // GET /api/log/motor/{motorId}
        [HttpGet("motor/{motorId}")]
        public async Task<IActionResult> GetByMotor(string motorId)
        {
            if (!Guid.TryParse(motorId, out var mId)) return BadRequest("Invalid id.");
            var logs = await _logService.GetLogsAsync(mId, 50);
            return Ok(logs);
        }

        // DELETE /api/log/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOne(string id)
        {
            if (!Guid.TryParse(id, out var logId)) return BadRequest("Invalid id.");
            var deleted = await _logService.DeleteLogAsync(logId);
            if (!deleted) return NotFound();
            return NoContent();
        }

        // DELETE /api/log
        [HttpDelete]
        public async Task<IActionResult> ClearAll()
        {
            var count = await _logService.ClearAllLogsAsync();
            return Ok(new { deleted = count });
        }
    }
}
