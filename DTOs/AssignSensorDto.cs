namespace iTarlaMapBackend.DTOs
{
    public class AssignSensorDto
    {
        public string DeviceCode { get; set; } = null!;
        public Guid FarmId { get; set; }
        public string Type { get; set; } = null!;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime InstallationDate { get; set; }
    }
}