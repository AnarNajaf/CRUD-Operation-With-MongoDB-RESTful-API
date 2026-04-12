using iTarlaMapBackend.DTOs;
using iTarlaMapBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iTarlaMapBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FarmController : ControllerBase
    {
        private readonly FarmService _farmService;
        private readonly FarmerService _farmerService;

        public FarmController(FarmService farmService, FarmerService farmerService)
        {
            _farmService = farmService;
            _farmerService = farmerService;
        }

        private async Task<Guid> GetCurrentFarmerIdAsync()
        {
            var farmer = await _farmerService.GetOrCreateFromClaimsAsync(User);
            return farmer.Id;
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyFarms()
        {
            var farmerId = await GetCurrentFarmerIdAsync();
            var farms = await _farmService.GetByFarmerIdAsync(farmerId);
            return Ok(farms);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var farmId))
                return BadRequest("Invalid farm id.");

            var farm = await _farmService.GetByIdAndFarmerIdAsync(farmId, farmerId);

            if (farm == null)
                return NotFound("Farm not found.");

            return Ok(farm);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFarmDto dto)
        {
            var farmerId = await GetCurrentFarmerIdAsync();
            var farm = await _farmService.CreateAsync(farmerId, dto);
            return Ok(farm);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateFarmDto dto)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var farmId))
                return BadRequest("Invalid farm id.");

            var updated = await _farmService.UpdateAsync(farmId, farmerId, dto);

            if (!updated)
                return NotFound("Farm not found or does not belong to farmer.");

            return Ok("Farm updated successfully.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var farmId))
                return BadRequest("Invalid farm id.");

            var deleted = await _farmService.RemoveAsync(farmId, farmerId);

            if (!deleted)
                return NotFound("Farm not found or does not belong to farmer.");

            return Ok("Selected farm deleted.");
        }

        [HttpPatch("{id}/color")]
        public async Task<IActionResult> UpdateFarmColor(string id, [FromBody] UpdateFarmColor dto)
        {
            var farmerId = await GetCurrentFarmerIdAsync();

            if (!Guid.TryParse(id, out var farmId))
                return BadRequest("Invalid farm id.");

            var updated = await _farmService.UpdateFarmColorAsync(farmId, farmerId, dto.Color);

            if (!updated)
                return NotFound("Farm not found or does not belong to farmer.");

            return Ok("Farm color updated successfully.");
        }
    }
}