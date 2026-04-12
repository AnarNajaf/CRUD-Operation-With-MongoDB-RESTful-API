using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace iTarlaMapBackend.Models
{
    public class Farm
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();
        [BsonRepresentation(BsonType.String)]
        public Guid FarmerId { get; set; } 
        public string? Name { get; set; }
        public string? Color { get; set; }
        public string? ResponsiblePerson { get; set; }
        public string? FarmType { get; set; }
        public List<Coordinate> Polygon { get; set; } = new();
        public List<Motor> motors { get; set; } = new List<Motor>();
        public List<Sensor> sensors { get; set; } = new List<Sensor>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}