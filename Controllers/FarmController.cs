using System;
using iTarlaMapBackend.Models;
using iTarlaMapBackend.Services;
using Microsoft.AspNetCore.Mvc;
using iTarlaMapBackend.Controllers;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace iTarlaMapBackend.Controllers;
[Controller]
[Route("api/[controller]")]
public class FarmController: Controller
{
    private readonly FarmService _farmService;
    public FarmController(FarmService farmService)
    {
        _farmService = farmService;
    }
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var farms = await _farmService.GetAsync();
        return Ok(farms);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var selectedfarm = await _farmService.GetAsync(id);
        return Ok(selectedfarm);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody]Farm farm)
    {
        await _farmService.CreateAsync(farm);
        return CreatedAtAction(nameof(Get), new {id = farm.Id}, farm);
    }
    //Update
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id,[FromBody] Farm farm)
    {
        await _farmService.UpdateAsync(id, farm);
        return Ok(farm);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _farmService.RemoveAsync(id);
        return Ok("Selected Farm Deleted");
    }
}