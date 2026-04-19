using iTarlaMapBackend.DTOs;
using iTarlaMapBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace iTarlaMapBackend.Services
{
    public class ScheduleService
    {
        private readonly IMongoCollection<Schedule> _schedules;

        public ScheduleService(IOptions<iTarlaDbConnection> dbSettings)
        {
            var settings = dbSettings.Value;
            var client = new MongoClient(settings.ConnectionString);
            var db = client.GetDatabase(settings.DatabaseName);
            _schedules = db.GetCollection<Schedule>("Schedules");
        }

        public async Task<Schedule?> GetByMotorIdAsync(Guid motorId, Guid farmerId) =>
            await _schedules.Find(s => s.MotorId == motorId && s.FarmerId == farmerId)
                .FirstOrDefaultAsync();

        // Used by the watchdog — no farmerId scope needed
        public async Task<Schedule?> GetByMotorIdDirectAsync(Guid motorId) =>
            await _schedules.Find(s => s.MotorId == motorId && s.IsEnabled).FirstOrDefaultAsync();

        public async Task<List<Schedule>> GetAllEnabledAsync() =>
            await _schedules.Find(s => s.IsEnabled).ToListAsync();

        public async Task<Schedule> CreateAsync(Guid farmerId, CreateScheduleDto dto)
        {
            await _schedules.DeleteOneAsync(s => s.MotorId == dto.MotorId && s.FarmerId == farmerId);

            var schedule = new Schedule
            {
                Id = Guid.NewGuid(),
                MotorId = dto.MotorId,
                FarmerId = farmerId,
                ScheduleType = dto.ScheduleType,
                IntervalHours = dto.IntervalHours,
                DurationMinutes = dto.DurationMinutes,
                TimeWindows = dto.TimeWindows
                    .Select(w => new Models.TimeWindow { StartTime = w.StartTime, DurationMinutes = w.DurationMinutes })
                    .ToList(),
                MaxRuntimeMinutes = dto.MaxRuntimeMinutes,
                ForbiddenFromHour = dto.ForbiddenFromHour,
                ForbiddenToHour = dto.ForbiddenToHour,
                LinkedSensorCode = dto.LinkedSensorCode,
                DataFreshnessMinutes = dto.DataFreshnessMinutes,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            await _schedules.InsertOneAsync(schedule);
            return schedule;
        }

        public async Task<bool> ToggleAsync(Guid scheduleId, Guid farmerId)
        {
            var schedule = await _schedules
                .Find(s => s.Id == scheduleId && s.FarmerId == farmerId)
                .FirstOrDefaultAsync();

            if (schedule == null) return false;

            var update = Builders<Schedule>.Update
                .Set(s => s.IsEnabled, !schedule.IsEnabled);

            await _schedules.UpdateOneAsync(s => s.Id == scheduleId, update);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid scheduleId, Guid farmerId)
        {
            var result = await _schedules.DeleteOneAsync(
                s => s.Id == scheduleId && s.FarmerId == farmerId);
            return result.DeletedCount > 0;
        }

        public async Task UpdateLastRanAtAsync(Guid scheduleId)
        {
            await _schedules.UpdateOneAsync(
                s => s.Id == scheduleId,
                Builders<Schedule>.Update.Set(s => s.LastRanAt, DateTime.UtcNow)
            );
        }

        // Records that a specific time window (e.g. "06:00") ran today
        public async Task UpdateTimeWindowLastRunAsync(Guid scheduleId, string windowKey)
        {
            await _schedules.UpdateOneAsync(
                s => s.Id == scheduleId,
                Builders<Schedule>.Update.Set($"LastRunDates.{windowKey}", DateTime.UtcNow)
            );
        }
    }
}