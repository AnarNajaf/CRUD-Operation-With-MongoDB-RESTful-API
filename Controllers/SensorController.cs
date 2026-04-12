using iTarlaMapBackend.DTOs;
using iTarlaMapBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iTarlaMapBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SensorController : ControllerBase
    {
        private readonly DeviceService _deviceService;
        private readonly FarmerService _farmerService;

        public SensorController(DeviceService deviceService, FarmerService farmerService)
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
        public async Task<IActionResult> GetMySensors()
        {
            var farmerId = await GetCurrentFarmerIdAsync();
            var sensors = await _deviceService.GetSensorsByFarmerIdAsync(farmerId);
            return Ok(sensors);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var sensorId))
                return BadRequest("Invalid sensor id.");

            var sensor = await _deviceService.GetSensorByIdAsync(sensorId, farmerId);

            if (sensor == null)
                return NotFound("Sensor not found.");

            return Ok(sensor);
        }

        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] AssignSensorDto dto)
        {
            var farmerId = await GetCurrentFarmerIdAsync();
            var sensor = await _deviceService.AssignSensorAsync(farmerId, dto);
            return Ok(sensor);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateSensorDto dto)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var sensorId))
                return BadRequest("Invalid sensor id.");

            var updated = await _deviceService.UpdateSensorAsync(sensorId, farmerId, dto);

            if (!updated)
                return NotFound("Sensor not found or does not belong to farmer.");

            return Ok("Sensor updated successfully.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var sensorId))
                return BadRequest("Invalid sensor id.");

            var deleted = await _deviceService.RemoveSensorAsync(sensorId, farmerId);

            if (!deleted)
                return NotFound("Sensor not found or does not belong to farmer.");

            return Ok("Selected sensor deleted.");
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateSensorStatus(string id, [FromBody] UpdateSensorStatus dto)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var sensorId))
                return BadRequest("Invalid sensor id.");

            var result = await _deviceService.UpdateSensorStatusAsync(sensorId, farmerId, dto.IsActive);

            if (!result.success)
                return BadRequest(result.message);

            return Ok(new { isActive = result.isActive });
        }
    }
}