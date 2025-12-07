using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace iTarlaMapBackend.Models;

public class Sensor
{
    [BsonId]
    [BsonRepresentation(BsonType.String)] 
    public Guid Sensor_Id { get; set; }
    public string Type { get; set; } = null!;
    public double Lat { get; set; }
    public double Lng { get; set; }
    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime InstallationDate { get; set; } = new DateTime();
    public bool IsActive { get; set; }
    public string FarmId{get;set;}=null!;
}