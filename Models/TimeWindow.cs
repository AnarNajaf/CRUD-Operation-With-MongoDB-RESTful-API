namespace iTarlaMapBackend.Models
{
    public class TimeWindow
    {
        public string StartTime { get; set; } = ""; // "HH:mm" UTC
        public int DurationMinutes { get; set; }
    }
}
