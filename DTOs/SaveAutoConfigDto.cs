namespace iTarlaMapBackend.DTOs
{
    public class SaveAutoConfigDto
    {
        public List<string> LinkedSensorCodes { get; set; } = new();
        public double LowerThreshold { get; set; } = 30;
        public double UpperThreshold { get; set; } = 60;
        public int AutoMaxRuntimeMinutes { get; set; } = 60;
    }
}
