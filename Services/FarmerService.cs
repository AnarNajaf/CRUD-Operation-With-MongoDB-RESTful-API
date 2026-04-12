using System.Security.Claims;
using iTarlaMapBackend.DTOs;
using iTarlaMapBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace iTarlaMapBackend.Services
{
    public class FarmerService
    {
        private readonly IMongoCollection<Farmer> _farmersCollection;

        public FarmerService(IOptions<iTarlaDbConnection> dbSettings)
        {
            var settings = dbSettings.Value;

            var mongoClient = new MongoClient(settings.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);
            _farmersCollection = mongoDatabase.GetCollection<Farmer>(settings.FarmersCollectionName);

            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var authUserIdIndex = new CreateIndexModel<Farmer>(
                Builders<Farmer>.IndexKeys.Ascending(f => f.AuthUserId),
                new CreateIndexOptions { Unique = true }
            );

            _farmersCollection.Indexes.CreateOne(authUserIdIndex);
        }

        public async Task<Farmer?> GetAsync(string id)
        {
            if (!Guid.TryParse(id, out Guid guidId))
                throw new ArgumentException("Invalid GUID format", nameof(id));

            return await _farmersCollection.Find(x => x.Id == guidId).FirstOrDefaultAsync();
        }

        public async Task<List<Farmer>> GetAsync()
        {
            return await _farmersCollection.Find(_ => true).ToListAsync();
        }

        public async Task<Farmer?> GetByAuthUserIdAsync(string authUserId)
        {
            return await _farmersCollection
                .Find(f => f.AuthUserId == authUserId)
                .FirstOrDefaultAsync();
        }

        public async Task<Farmer> GetOrCreateFromClaimsAsync(ClaimsPrincipal user)
        {
            var authUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(authUserId))
                throw new UnauthorizedAccessException("JWT does not contain NameIdentifier.");

            var firstName = user.FindFirstValue(ClaimTypes.Name) ?? "";
            var surname = user.FindFirstValue(ClaimTypes.Surname) ?? "";
            var email = user.FindFirstValue(ClaimTypes.Email) ?? "";
            string fullName;

            fullName = $"{firstName} {surname}".Trim();
           if (!string.IsNullOrWhiteSpace(firstName) && !firstName.Contains("@"))
    fullName = $"{firstName} {surname}".Trim();
    else if (!string.IsNullOrWhiteSpace(surname))
    fullName = surname;
    else
    fullName = email;
            var now = DateTime.UtcNow;

            var filter = Builders<Farmer>.Filter.Eq(f => f.AuthUserId, authUserId);

            var existing = await _farmersCollection.Find(filter).FirstOrDefaultAsync();
            if (existing != null)
                return existing;

            var newFarmer = new Farmer
            {
                Id = Guid.NewGuid(),
                AuthUserId = authUserId,
                FullName = fullName,
                Email = email,
                PhoneNumber = "",
                Address = "",
                CreatedAt = now,
                UpdatedAt = now,
                FarmIds = new List<string>()
            };

            try
            {
                await _farmersCollection.InsertOneAsync(newFarmer);
                return newFarmer;
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                var createdByAnotherRequest = await _farmersCollection.Find(filter).FirstOrDefaultAsync();
                if (createdByAnotherRequest != null)
                    return createdByAnotherRequest;

                throw;
            }
        }

        public async Task CreateAsync(Farmer newFarmer)
        {
            if (newFarmer.Id == Guid.Empty)
                newFarmer.Id = Guid.NewGuid();

            newFarmer.CreatedAt = DateTime.UtcNow;
            newFarmer.UpdatedAt = DateTime.UtcNow;

            await _farmersCollection.InsertOneAsync(newFarmer);
        }

        public async Task UpdateAsync(string farmerId, Farmer updatedFarmer)
        {
            if (!Guid.TryParse(farmerId, out Guid guidId))
                throw new ArgumentException("Invalid GUID format", nameof(farmerId));

            updatedFarmer.Id = guidId;
            updatedFarmer.UpdatedAt = DateTime.UtcNow;

            var filter = Builders<Farmer>.Filter.Eq(f => f.Id, guidId);
            await _farmersCollection.ReplaceOneAsync(filter, updatedFarmer);
        }

        public async Task RemoveAsync(string id)
        {
            if (!Guid.TryParse(id, out Guid guidId))
                throw new ArgumentException("Invalid GUID format", nameof(id));

            var filter = Builders<Farmer>.Filter.Eq(f => f.Id, guidId);
            await _farmersCollection.DeleteOneAsync(filter);
        }
    }
}