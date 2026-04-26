using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace iTarlaMapBackend.Models
{
    public class Schedule
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid MotorId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid FarmerId { get; set; }

        // "interval" = every X hours | "time" = specific times of day
        public string ScheduleType { get; set; } = "interval";

        // Interval mode
        public int IntervalHours { get; set; } = 3;
        public int DurationMinutes { get; set; }
        public DateTime? LastRanAt { get; set; }

        // Time-of-day mode
        public List<TimeWindow> TimeWindows { get; set; } = new();
        // Tracks last run per window key ("06:00", "18:00", …) to avoid double-firing
        public Dictionary<string, DateTime> LastRunDates { get; set; } = new();

        // ── Safety rules ─────────────────────────────────────────────────────
        // 0 = no cap; if set, motor is forced off after this many minutes
        public int MaxRuntimeMinutes { get; set; } = 0;
        // UTC hours (0-23). Don't trigger if current hour falls in [From, To)
        public int? ForbiddenFromHour { get; set; }
        public int? ForbiddenToHour { get; set; }
        // If set, skip trigger when this sensor's Firebase data is older than DataFreshnessMinutes
        public string? LinkedSensorCode { get; set; }
        public int DataFreshnessMinutes { get; set; } = 0; // 0 = skip check

        // Day-of-week filter: 0=Sun,1=Mon,...,6=Sat. Empty list = all days allowed.
        public List<int> AllowedDays { get; set; } = new();

        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}