using iTarlaMapBackend.DTOs;
using iTarlaMapBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace iTarlaMapBackend.Services
{
    public class FarmService
    {
        private readonly IMongoCollection<Farm> _farmCollection;

        public FarmService(IOptions<iTarlaDbConnection> dbSettings)
        {
            var settings = dbSettings.Value;
            var mongoClient = new MongoClient(settings.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);
            _farmCollection = mongoDatabase.GetCollection<Farm>(settings.FarmsCollectionName);
        }

        public async Task<List<Farm>> GetByFarmerIdAsync(Guid farmerId)
        {
            return await _farmCollection
                .Find(f => f.FarmerId == farmerId)
                .ToListAsync();
        }

        public async Task<Farm?> GetByIdAndFarmerIdAsync(Guid farmId, Guid farmerId)
        {
            return await _farmCollection
                .Find(f => f.Id == farmId && f.FarmerId == farmerId)
                .FirstOrDefaultAsync();
        }

        public async Task<Farm> CreateAsync(Guid farmerId, CreateFarmDto dto)
        {
            var farm = new Farm
            {
                Id = Guid.NewGuid(),
                FarmerId = farmerId,
                Name = dto.Name,
                Color = dto.Color,
                ResponsiblePerson = dto.ResponsiblePerson,
                FarmType = dto.FarmType,
                Polygon = dto.Polygon,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _farmCollection.InsertOneAsync(farm);
            return farm;
        }

        public async Task<bool> UpdateAsync(Guid farmId, Guid farmerId, UpdateFarmDto dto)
        {
            var update = Builders<Farm>.Update
                .Set(f => f.Name, dto.Name)
                .Set(f => f.Color, dto.Color)
                .Set(f => f.ResponsiblePerson, dto.ResponsiblePerson)
                .Set(f => f.FarmType, dto.FarmType)
                .Set(f => f.Polygon, dto.Polygon)
                .Set(f => f.UpdatedAt, DateTime.UtcNow);

            var result = await _farmCollection.UpdateOneAsync(
                f => f.Id == farmId && f.FarmerId == farmerId,
                update
            );

            return result.ModifiedCount > 0;
        }

        public async Task<bool> RemoveAsync(Guid farmId, Guid farmerId)
        {
            var result = await _farmCollection.DeleteOneAsync(
                f => f.Id == farmId && f.FarmerId == farmerId
            );

            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateFarmColorAsync(Guid farmId, Guid farmerId, string newColor)
        {
            var update = Builders<Farm>.Update
                .Set(f => f.Color, newColor)
                .Set(f => f.UpdatedAt, DateTime.UtcNow);

            var result = await _farmCollection.UpdateOneAsync(
                f => f.Id == farmId && f.FarmerId == farmerId,
                update
            );

            return result.ModifiedCount > 0;
        }
    }
}