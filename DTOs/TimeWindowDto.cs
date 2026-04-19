namespace iTarlaMapBackend.DTOs
{
    public class TimeWindowDto
    {
        public string StartTime { get; set; } = ""; // "HH:mm" UTC
        public int DurationMinutes { get; set; }
    }
}
