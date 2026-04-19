using iTarlaMapBackend.DTOs;
using iTarlaMapBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iTarlaMapBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScheduleController : ControllerBase
    {
        private readonly ScheduleService _scheduleService;
        private readonly FarmerService _farmerService;
        private readonly DeviceService _deviceService;

        public ScheduleController(ScheduleService scheduleService, FarmerService farmerService, DeviceService deviceService)
        {
            _scheduleService = scheduleService;
            _farmerService = farmerService;
            _deviceService = deviceService;
        }

        private async Task<Guid> GetFarmerIdAsync() =>
            (await _farmerService.GetOrCreateFromClaimsAsync(User)).Id;

        [HttpGet("motor/{motorId}")]
        public async Task<IActionResult> GetByMotor(string motorId)
        {
            var farmerId = await GetFarmerIdAsync();
            if (!Guid.TryParse(motorId, out var mId)) return BadRequest("Invalid motor id.");
            var schedule = await _scheduleService.GetByMotorIdAsync(mId, farmerId);
            if (schedule == null) return NotFound("No schedule found.");
            return Ok(schedule);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateScheduleDto dto)
        {
            var farmerId = await GetFarmerIdAsync();
            var schedule = await _scheduleService.CreateAsync(farmerId, dto);
            return Ok(schedule);
        }

        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> Toggle(string id)
        {
            var farmerId = await GetFarmerIdAsync();
            if (!Guid.TryParse(id, out var scheduleId)) return BadRequest("Invalid id.");

            var toggled = await _scheduleService.ToggleAsync(scheduleId, farmerId);
            if (!toggled) return NotFound();

            // Keep motor mode in sync with schedule enabled state
            var schedule = await _scheduleService.GetByIdAsync(scheduleId, farmerId);
            if (schedule != null)
            {
                var targetMode = schedule.IsEnabled ? "scheduled" : "manual";
                await _deviceService.UpdateMotorModeAsync(schedule.MotorId, farmerId, targetMode);
            }

            return Ok("Schedule toggled.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var farmerId = await GetFarmerIdAsync();
            if (!Guid.TryParse(id, out var scheduleId)) return BadRequest("Invalid id.");

            // Fetch before deleting so we have the motorId
            var schedule = await _scheduleService.GetByIdAsync(scheduleId, farmerId);
            var deleted = await _scheduleService.DeleteAsync(scheduleId, farmerId);
            if (!deleted) return NotFound();

            // Revert motor to manual — no schedule, no automation
            if (schedule != null)
                await _deviceService.UpdateMotorModeAsync(schedule.MotorId, farmerId, "manual");

            return Ok("Schedule deleted.");
        }
    }
}