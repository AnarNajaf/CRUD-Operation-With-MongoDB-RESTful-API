using iTarlaMapBackend.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
namespace iTarlaMapBackend.Models;
public class Farmer
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = null!;

    public string Email { get; set; }= null!;

    public string PhoneNumber { get; set; }=null!;

    public string Address { get; set; }

    public List<string> FarmIds { get; set; } = new List<string>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}