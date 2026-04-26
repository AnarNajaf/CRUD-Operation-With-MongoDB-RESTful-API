using iTarlaMapBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace iTarlaMapBackend.Services
{
    public class LogService
    {
        private readonly IMongoCollection<IrrigationLog> _logs;

        public LogService(IOptions<iTarlaDbConnection> dbSettings)
        {
            var settings = dbSettings.Value;
            var client = new MongoClient(settings.ConnectionString);
            var db = client.GetDatabase(settings.DatabaseName);
            _logs = db.GetCollection<IrrigationLog>(settings.LogsCollectionName);
        }

        public async Task LogAsync(Guid motorId, string deviceCode, string eventType, bool success, string? error = null)
        {
            await _logs.InsertOneAsync(new IrrigationLog
            {
                Id = Guid.NewGuid(),
                MotorId = motorId,
                DeviceCode = deviceCode,
                Event = eventType,
                Success = success,
                ErrorMessage = error,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task<List<IrrigationLog>> GetLogsAsync(Guid motorId, int limit = 50)
        {
            return await _logs
                .Find(l => l.MotorId == motorId)
                .SortByDescending(l => l.Timestamp)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<List<IrrigationLog>> GetAllLogsAsync(int skip = 0, int limit = 50)
        {
            return await _logs
                .Find(_ => true)
                .SortByDescending(l => l.Timestamp)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<long> CountAllLogsAsync()
        {
            return await _logs.CountDocumentsAsync(_ => true);
        }

        public async Task<bool> DeleteLogAsync(Guid id)
        {
            var result = await _logs.DeleteOneAsync(l => l.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<long> ClearAllLogsAsync()
        {
            var result = await _logs.DeleteManyAsync(_ => true);
            return result.DeletedCount;
        }
    }
}