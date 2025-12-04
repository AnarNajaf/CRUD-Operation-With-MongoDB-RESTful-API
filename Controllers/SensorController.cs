using System;
using iTarlaMapBackend.Models;
using iTarlaMapBackend.Services;
using Microsoft.AspNetCore.Mvc;
using iTarlaMapBackend.Controllers;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Collections.Generic;

namespace iTarlaMapBackend.Controllers;
[Controller]
[Route("api/[controller]")]
public class SensorController: Controller
{
    private readonly DeviceService _sensorService;
    public SensorController(DeviceService sensorService)
    {
        _sensorService = sensorService;
    }
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var sensors = await _sensorService.GetSensorsAsync();
        return Ok(sensors);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var selectedsensor = await _sensorService.GetSensorAsync(id);
        return Ok(selectedsensor);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody]Sensor sensor)
    {
        await _sensorService.CreateSensorAsync(sensor);
        return CreatedAtAction(nameof(Get), new {id = sensor.Sensor_Id}, sensor);
    }
    //Update
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id,[FromBody] Sensor sensor)
    {
        await _sensorService.UpdateSensorAsync(id, sensor);
        return Ok(sensor);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _sensorService.RemoveSensorAsync(id);
        return Ok("Selected Sensor Deleted");
    }
}