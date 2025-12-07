using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using iTarlaMapBackend.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using iTarlaMapBackend.Models;
using Microsoft.VisualBasic;
namespace iTarlaMapBackend.Controllers
{
    [Controller]
    [Route("api/[controller]")]
    public class FarmerController : Controller
    {
        private readonly FarmerService _farmerService;

        public FarmerController(FarmerService farmerService)
        {
            _farmerService = farmerService;
        }
        //Create
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Farmer farmer)
        {
            await _farmerService.CreateAsync(farmer);
            return CreatedAtAction(nameof(Get), new { id = farmer.Id }, farmer);
        }
        //Read
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var selectedfarmer = await _farmerService.GetAsync(id);
            return Ok(selectedfarmer);
        }
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var farmers  = await _farmerService.GetAsync();
            return Ok(farmers);
        }
        //Update
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Farmer farmer)
        {
            await _farmerService.UpdateAsync(id, farmer);
            return Ok();
        }
        //Delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFarmer(string id)
        {
            await _farmerService.RemoveAsync(id);
            return NoContent();
        }
    }
}