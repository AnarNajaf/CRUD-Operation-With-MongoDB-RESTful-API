using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace iTarlaMapBackend.Models
{
    public class Motor
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        [BsonRepresentation(BsonType.String)]

        public string DeviceCode { get; set; } = null!;
        [BsonRepresentation(BsonType.String)]
        public Guid FarmerId { get; set; }
        public Guid FarmId { get; set; }

        public string Type { get; set; } = null!;
        public double Lat { get; set; }
        public double Lng { get; set; }

        public DateTime InstallationDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }
        public DateTime? ActiveSince { get; set; } // set when turned ON, cleared when turned OFF
        public string Mode { get; set; } = "manual"; // "manual" | "scheduled" | "auto"

        // Auto mode config
        public List<string> LinkedSensorCodes { get; set; } = new();
        public double LowerThreshold { get; set; } = 30;   // turn ON below this %
        public double UpperThreshold { get; set; } = 60;   // turn OFF above this %
        public int AutoMaxRuntimeMinutes { get; set; } = 60;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}