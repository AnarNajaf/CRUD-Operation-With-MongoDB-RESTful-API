namespace iTarlaMapBackend.DTOs
{
    public class CreateScheduleDto
    {
        public Guid MotorId { get; set; }
        public string ScheduleType { get; set; } = "interval"; // "interval" | "time"

        // Interval mode
        public int IntervalHours { get; set; } = 3;
        public int DurationMinutes { get; set; }

        // Time-of-day mode
        public List<TimeWindowDto> TimeWindows { get; set; } = new();

        // Safety rules
        public int MaxRuntimeMinutes { get; set; } = 0;
        public int? ForbiddenFromHour { get; set; }
        public int? ForbiddenToHour { get; set; }
        public string? LinkedSensorCode { get; set; }
        public int DataFreshnessMinutes { get; set; } = 0;
    }
}