using iTarlaMapBackend.Services;
using iTarlaMapBackend.Controllers;
using System;
using Microsoft.AspNetCore.Mvc;
using iTarlaMapBackend.Models;
[Controller]
[Route("api/[controller]")]
public class MotorController: Controller
{
    private readonly DeviceService _motorService;
    public MotorController(DeviceService motorService)
    {
        _motorService=motorService;
    }
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var motors = await _motorService.GetMotorsAsync();
        return Ok(motors);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var selectedmotor = await _motorService.GetMotorAsync(id);
        return Ok(selectedmotor);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody]Motor motor)
    {
        await _motorService.CreateMotorAsync(motor);
        return CreatedAtAction(nameof(Get), new {id = motor.Motor_Id}, motor);
    }
    //Update
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id,[FromBody] Motor motor)
    {
        await _motorService.UpdateMotorAsync(id, motor);
        return Ok(motor);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _motorService.RemoveMotorAsync(id);
        return Ok("Selected Motor Deleted");
    }
}