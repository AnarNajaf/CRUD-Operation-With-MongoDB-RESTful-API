using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using iTarlaMapBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
namespace iTarlaMapBackend.Services;
public class DeviceService
{
    private readonly IMongoCollection<Sensor> _sensorCollection;
    private readonly IMongoCollection<Motor> _motorCollection;
    public DeviceService(IOptions<iTarlaDbConnection> dbSettings)
    {
        var settings = dbSettings.Value;
        var mongoClient = new MongoClient(settings.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);
        _sensorCollection = mongoDatabase.GetCollection<Sensor>(settings.SensorsCollectionName);
        _motorCollection = mongoDatabase.GetCollection<Motor>(settings.MotorsCollectionName);
    }
    public async Task<Sensor?> GetSensorAsync(string id)
    {
        if(!Guid.TryParse(id, out Guid guidId))
        throw new ArgumentException("Invalid GUID format", nameof(id));
        return await _sensorCollection.Find(s => s.Sensor_Id == guidId).FirstOrDefaultAsync();
    }
    public async Task<List<Sensor>> GetSensorsAsync()
    {
        return await _sensorCollection.Find(_ => true).ToListAsync();
    }
    public async Task<Motor?> GetMotorAsync(string id)
    {
        if(!Guid.TryParse(id, out Guid guidId))
        throw new ArgumentException("Invalid GUID format", nameof(id));
        return await _motorCollection.Find(m => m.Motor_Id == guidId).FirstOrDefaultAsync();
    }
    public async Task<List<Motor>> GetMotorsAsync()
    {
        return await _motorCollection.Find(_ => true).ToListAsync();
    }
    public async Task CreateSensorAsync(Sensor newSensor)
    {
        if (newSensor.Sensor_Id == Guid.Empty)
            newSensor.Sensor_Id = Guid.NewGuid();
        await _sensorCollection.InsertOneAsync(newSensor);
    }
    public async Task CreateMotorAsync(Motor newMotor)
    {
        if (newMotor.Motor_Id == Guid.Empty)
            newMotor.Motor_Id = Guid.NewGuid();
        await _motorCollection.InsertOneAsync(newMotor);
    }
    public async Task UpdateSensorAsync(string sensorId, Sensor updatedSensor)
    {
        if(!Guid.TryParse(sensorId, out Guid guidId))
        throw new ArgumentException("Invalid GUID format", nameof(sensorId));
        updatedSensor.Sensor_Id = guidId;
        var filter = Builders<Sensor>.Filter.Eq(s => s.Sensor_Id, guidId);
        await _sensorCollection.ReplaceOneAsync(filter, updatedSensor);
    }
    public async Task UpdateMotorAsync(string motorId, Motor updatedMotor)
    {
        if(!Guid.TryParse(motorId, out Guid guidId))
        throw new ArgumentException("Invalid GUID format", nameof(motorId));
        updatedMotor.Motor_Id = guidId;
        var filter = Builders<Motor>.Filter.Eq(m => m.Motor_Id, guidId);
        await _motorCollection.ReplaceOneAsync(filter, updatedMotor);
    }
    public async Task RemoveSensorAsync(string id)
    {
        if(!Guid.TryParse(id, out Guid guidId))
        throw new ArgumentException("Invalid GUID format", nameof(id));
        var filter = Builders<Sensor>.Filter.Eq(s => s.Sensor_Id, guidId);
        await _sensorCollection.DeleteOneAsync(filter);
    }
    public async Task RemoveMotorAsync(string id)
    {
        if(!Guid.TryParse(id, out Guid guidId))
        throw new ArgumentException("Invalid GUID format", nameof(id));
        var filter = Builders<Motor>.Filter.Eq(m => m.Motor_Id, guidId);
        await _motorCollection.DeleteOneAsync(filter);
    }
    
}