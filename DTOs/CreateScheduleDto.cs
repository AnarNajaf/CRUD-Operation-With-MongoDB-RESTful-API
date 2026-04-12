namespace iTarlaMapBackend.DTOs
{
    public class CreateScheduleDto
    {
        public Guid MotorId { get; set; }
        public int IntervalHours { get; set; } = 3;
        public int DurationMinutes { get; set; }
    }
}