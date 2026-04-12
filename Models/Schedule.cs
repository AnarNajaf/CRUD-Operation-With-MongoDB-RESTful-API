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

        public int IntervalHours { get; set; } = 3; 
        public int DurationMinutes { get; set; }    
        public bool IsEnabled { get; set; } = true;
        public DateTime? LastRanAt { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}