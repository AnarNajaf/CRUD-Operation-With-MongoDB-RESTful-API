using iTarlaMapBackend.DTOs;
using iTarlaMapBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace iTarlaMapBackend.Services
{
    public class DeviceService
    {
        private readonly IMongoCollection<Sensor> _sensorCollection;
        private readonly IMongoCollection<Motor> _motorCollection;
        private readonly IMongoCollection<Farm> _farmCollection;

        public DeviceService(IOptions<iTarlaDbConnection> dbSettings)
        {
            var settings = dbSettings.Value;
            var mongoClient = new MongoClient(settings.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);

            _sensorCollection = mongoDatabase.GetCollection<Sensor>(settings.SensorsCollectionName);
            _motorCollection = mongoDatabase.GetCollection<Motor>(settings.MotorsCollectionName);
            _farmCollection = mongoDatabase.GetCollection<Farm>(settings.FarmsCollectionName);
        }

        // ---------------- SENSORS ----------------

        public async Task<List<Sensor>> GetSensorsByFarmerIdAsync(Guid farmerId)
        {
            return await _sensorCollection
                .Find(s => s.FarmerId == farmerId)
                .ToListAsync();
        }

        public async Task<List<Sensor>> GetSensorsByFarmIdAsync(Guid farmId, Guid farmerId)
        {
            return await _sensorCollection
                .Find(s => s.FarmId == farmId && s.FarmerId == farmerId)
                .ToListAsync();
        }

        public async Task<Sensor?> GetSensorByIdAsync(Guid sensorId, Guid farmerId)
        {
            return await _sensorCollection
                .Find(s => s.Id == sensorId && s.FarmerId == farmerId)
                .FirstOrDefaultAsync();
        }

        public async Task<Sensor> AssignSensorAsync(Guid farmerId, AssignSensorDto dto)
        {
            var farm = await _farmCollection
                .Find(f => f.Id == dto.FarmId && f.FarmerId == farmerId)
                .FirstOrDefaultAsync();

            if (farm == null)
                throw new Exception("Farm not found or does not belong to farmer.");

            // Prevent any user from registering a device code already used by anyone
            var duplicate = await _sensorCollection
                .Find(s => s.DeviceCode == dto.DeviceCode)
                .FirstOrDefaultAsync();
            if (duplicate != null)
                throw new Exception($"Sensor '{dto.DeviceCode}' is already registered.");

            var sensor = new Sensor
            {
                Id = Guid.NewGuid(),
                DeviceCode = dto.DeviceCode,
                FarmerId = farmerId,
                FarmId = dto.FarmId,
                Type = dto.Type,
                Lat = dto.Lat,
                Lng = dto.Lng,
                InstallationDate = dto.InstallationDate,
                IsActive = false,
                BatteryLevel = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _sensorCollection.InsertOneAsync(sensor);
            return sensor;
        }

        public async Task<bool> UpdateSensorAsync(Guid sensorId, Guid farmerId, UpdateSensorDto dto)
        {
            var update = Builders<Sensor>.Update
                .Set(s => s.Type, dto.Type)
                .Set(s => s.Lat, dto.Lat)
                .Set(s => s.Lng, dto.Lng)
                .Set(s => s.InstallationDate, dto.InstallationDate)
                .Set(s => s.IsActive, dto.IsActive)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            var result = await _sensorCollection.UpdateOneAsync(
                s => s.Id == sensorId && s.FarmerId == farmerId,
                update
            );

            return result.ModifiedCount > 0;
        }

        public async Task<bool> RemoveSensorAsync(Guid sensorId, Guid farmerId)
        {
            var result = await _sensorCollection.DeleteOneAsync(
                s => s.Id == sensorId && s.FarmerId == farmerId
            );

            return result.DeletedCount > 0;
        }

        public async Task<(bool success, bool isActive, string message)> UpdateSensorStatusAsync(Guid sensorId, Guid farmerId, bool requestedState)
        {
            var sensor = await _sensorCollection
                .Find(s => s.Id == sensorId && s.FarmerId == farmerId)
                .FirstOrDefaultAsync();

            if (sensor == null)
                return (false, false, "Sensor not found");

            if (requestedState && sensor.BatteryLevel < 10)
                return (false, sensor.IsActive, "Cannot activate sensor due to low battery");

            var update = Builders<Sensor>.Update
                .Set(s => s.IsActive, requestedState)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _sensorCollection.UpdateOneAsync(
                s => s.Id == sensorId && s.FarmerId == farmerId,
                update
            );

            return (true, requestedState, "Sensor status updated successfully");
        }

        // ---------------- MOTORS ----------------

        public async Task<List<Motor>> GetMotorsByFarmerIdAsync(Guid farmerId)
        {
            return await _motorCollection
                .Find(m => m.FarmerId == farmerId)
                .ToListAsync();
        }

        public async Task<List<Motor>> GetMotorsByFarmIdAsync(Guid farmId, Guid farmerId)
        {
            return await _motorCollection
                .Find(m => m.FarmId == farmId && m.FarmerId == farmerId)
                .ToListAsync();
        }

        public async Task<Motor?> GetMotorByIdAsync(Guid motorId, Guid farmerId)
        {
            return await _motorCollection
                .Find(m => m.Id == motorId && m.FarmerId == farmerId)
                .FirstOrDefaultAsync();
        }

        public async Task<Motor> AssignMotorAsync(Guid farmerId, AssignMotorDto dto)
        {
            var farm = await _farmCollection
                .Find(f => f.Id == dto.FarmId && f.FarmerId == farmerId)
                .FirstOrDefaultAsync();

            if (farm == null)
                throw new Exception("Farm not found or does not belong to farmer.");

            // Prevent any user from registering a device code already used by anyone
            var duplicate = await _motorCollection
                .Find(m => m.DeviceCode == dto.DeviceCode)
                .FirstOrDefaultAsync();
            if (duplicate != null)
                throw new Exception($"Motor '{dto.DeviceCode}' is already registered.");

            var motor = new Motor
            {
                Id = Guid.NewGuid(),
                DeviceCode = dto.DeviceCode,
                FarmerId = farmerId,
                FarmId = dto.FarmId,
                Type = dto.Type,
                Lat = dto.Lat,
                Lng = dto.Lng,
                InstallationDate = dto.InstallationDate,
                IsActive = false,
                Mode = "manual",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _motorCollection.InsertOneAsync(motor);
            return motor;
        }


        public async Task<bool> UpdateMotorAsync(Guid motorId, Guid farmerId, UpdateMotorDto dto)
        {
            var update = Builders<Motor>.Update
                .Set(m => m.Type, dto.Type)
                .Set(m => m.Lat, dto.Lat)
                .Set(m => m.Lng, dto.Lng)
                .Set(m => m.InstallationDate, dto.InstallationDate)
                .Set(m => m.IsActive, dto.IsActive)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            var result = await _motorCollection.UpdateOneAsync(
                m => m.Id == motorId && m.FarmerId == farmerId,
                update
            );

            return result.ModifiedCount > 0;
        }

        public async Task<(bool success, bool isActive, string message)> UpdateMotorStatusAsync(Guid motorId, Guid farmerId, bool requestedState)
        {
            var update = Builders<Motor>.Update
                .Set(m => m.IsActive, requestedState)
                .Set(m => m.ActiveSince, requestedState ? DateTime.UtcNow : (DateTime?)null)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            var result = await _motorCollection.UpdateOneAsync(
                m => m.Id == motorId && m.FarmerId == farmerId,
                update
            );

            if (result.MatchedCount == 0)
                return (false, false, $"Motor not found (id={motorId}, farmerId={farmerId})");

            return (true, requestedState, "Motor status updated successfully");
        }

        public async Task<List<Motor>> GetAllActiveMotorsAsync() =>
            await _motorCollection.Find(m => m.IsActive && m.ActiveSince != null).ToListAsync();

        public async Task<bool> RemoveMotorAsync(Guid motorId, Guid farmerId)
        {
            var result = await _motorCollection.DeleteOneAsync(
                m => m.Id == motorId && m.FarmerId == farmerId
            );

            return result.DeletedCount > 0;
        }
    public async Task<bool> UpdateMotorModeAsync(Guid motorId, Guid farmerId, string mode)
{
    var update = Builders<Motor>.Update
        .Set(m => m.Mode, mode)
        .Set(m => m.UpdatedAt, DateTime.UtcNow);

    var result = await _motorCollection.UpdateOneAsync(
        m => m.Id == motorId && m.FarmerId == farmerId,
        update
    );
    return result.ModifiedCount > 0;
}

    public async Task<Motor?> SaveAutoConfigAsync(Guid motorId, Guid farmerId, SaveAutoConfigDto dto)
    {
        var update = Builders<Motor>.Update
            .Set(m => m.LinkedSensorCodes, dto.LinkedSensorCodes)
            .Set(m => m.LowerThreshold, dto.LowerThreshold)
            .Set(m => m.UpperThreshold, dto.UpperThreshold)
            .Set(m => m.AutoMaxRuntimeMinutes, dto.AutoMaxRuntimeMinutes)
            .Set(m => m.Mode, "auto")
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        await _motorCollection.UpdateOneAsync(
            m => m.Id == motorId && m.FarmerId == farmerId,
            update
        );

        return await _motorCollection.Find(m => m.Id == motorId && m.FarmerId == farmerId).FirstOrDefaultAsync();
    }

    public async Task<List<Motor>> GetMotorsByModeAsync(string mode) =>
        await _motorCollection.Find(m => m.Mode == mode).ToListAsync();
    }

}