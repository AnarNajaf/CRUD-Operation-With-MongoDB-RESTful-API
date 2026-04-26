using iTarlaMapBackend.DTOs;
using iTarlaMapBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iTarlaMapBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MotorController : ControllerBase
    {
        private readonly DeviceService _deviceService;
        private readonly FarmerService _farmerService;

        public MotorController(DeviceService deviceService, FarmerService farmerService)
        {
            _deviceService = deviceService;
            _farmerService = farmerService;
        }

        private async Task<Guid> GetCurrentFarmerIdAsync()
        {
            var farmer = await _farmerService.GetOrCreateFromClaimsAsync(User);
            return farmer.Id;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyMotors()
        {
            var farmerId = await GetCurrentFarmerIdAsync();
            var motors = await _deviceService.GetMotorsByFarmerIdAsync(farmerId);
            return Ok(motors);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var motorId))
                return BadRequest("Invalid motor id.");

            var motor = await _deviceService.GetMotorByIdAsync(motorId, farmerId);

            if (motor == null)
                return NotFound("Motor not found.");

            return Ok(motor);
        }

        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] AssignMotorDto dto)
        {
            var farmerId = await GetCurrentFarmerIdAsync();
            try
            {
                var motor = await _deviceService.AssignMotorAsync(farmerId, dto);
                return Ok(motor);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateMotorDto dto)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var motorId))
                return BadRequest("Invalid motor id.");

            var updated = await _deviceService.UpdateMotorAsync(motorId, farmerId, dto);

            if (!updated)
                return NotFound("Motor not found or does not belong to farmer.");

            return Ok("Motor updated successfully.");
        }
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateMotorStatus(string id, [FromBody] UpdateMotorStatus dto)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var motorId))
                return BadRequest("Invalid motor id.");

            var result = await _deviceService.UpdateMotorStatusAsync(motorId, farmerId, dto.IsActive);

            if (!result.success)
                return BadRequest(result.message);

            return Ok(new { isActive = result.isActive });
        }
        [HttpPatch("{id}/mode")]
public async Task<IActionResult> UpdateMode(string id, [FromBody] UpdateMotorModeDto dto)
{
    var farmerId = await GetCurrentFarmerIdAsync();
    if (!Guid.TryParse(id, out var motorId)) return BadRequest("Invalid id.");
    var result = await _deviceService.UpdateMotorModeAsync(motorId, farmerId, dto.Mode);
    if (!result) return NotFound();
    return Ok(new { mode = dto.Mode });
}

        [HttpPatch("{id}/auto-config")]
        public async Task<IActionResult> SaveAutoConfig(string id, [FromBody] SaveAutoConfigDto dto)
        {
            var farmerId = await GetCurrentFarmerIdAsync();
            if (!Guid.TryParse(id, out var motorId)) return BadRequest("Invalid id.");
            var motor = await _deviceService.SaveAutoConfigAsync(motorId, farmerId, dto);
            if (motor == null) return NotFound();
            return Ok(motor);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var motorId))
                return BadRequest("Invalid motor id.");

            var deleted = await _deviceService.RemoveMotorAsync(motorId, farmerId);

            if (!deleted)
                return NotFound("Motor not found or does not belong to farmer.");

            return Ok("Selected motor deleted.");
        }
    }
}