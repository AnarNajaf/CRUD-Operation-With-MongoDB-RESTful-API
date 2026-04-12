namespace iTarlaMapBackend.DTOs
{
    public class UpdateMotorDto
    {
        public string Type { get; set; } = null!;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime InstallationDate { get; set; }
        public bool IsActive { get; set; }
    }
}