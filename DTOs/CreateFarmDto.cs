using iTarlaMapBackend.Models;

namespace iTarlaMapBackend.DTOs
{
    public class CreateFarmDto
    {
        public string Name { get; set; } = null!;
        public string? Color { get; set; }
        public string? ResponsiblePerson { get; set; }
        public string? FarmType { get; set; }
        public List<Coordinate> Polygon { get; set; } = new();
    }
}