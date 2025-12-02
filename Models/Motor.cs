using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace iTarlaMapBackend.Models;

public class Motor
{
    [BsonRepresentation(BsonType.ObjectId)]

    public Guid Motor_Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = null!;

    public double Lat { get; set; }
    public double Lng { get; set; }

    public DateTime InstallationDate { get; set; }
    public bool IsActive { get; set; }
}