using System;
using System.Runtime.CompilerServices;
using iTarlaMapBackend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
namespace iTarlaMapBackend.Services
{
    public class FarmerService
    {
      private readonly IMongoCollection<Farmer> _farmersCollection;

        public FarmerService(IOptions<iTarlaDbConnection> dbSettings)
        {
            var settings = dbSettings.Value; // get the configured iTarlaDbConnection

            var mongoClient = new MongoClient(settings.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(settings.DatabaseName);
            _farmersCollection = mongoDatabase.GetCollection<Farmer>(settings.FarmersCollectionName);
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

    public async Task CreateAsync(Farmer newFarmer)
    {
        if (newFarmer.Id == Guid.Empty)
            newFarmer.Id = Guid.NewGuid(); // Assign new GUID if not set

        await _farmersCollection.InsertOneAsync(newFarmer);
    }

    public async Task UpdateAsync(string farmerId, Farmer updatedFarmer)
        {
            if(!Guid.TryParse(farmerId, out Guid guidId))
            throw new ArgumentException("Invalid GUID format", nameof(farmerId));
            updatedFarmer.Id = guidId;
            var filter = Builders<Farmer>.Filter.Eq(f=> f.Id, guidId);
            await _farmersCollection.ReplaceOneAsync(filter,updatedFarmer);
        }

    public async Task RemoveAsync(string id)
    {
        if(!Guid.TryParse(id, out Guid guidId))
            throw new ArgumentException("Invalid GUID format", nameof(id));
        var filter = Builders<Farmer>.Filter.Eq(f => f.Id, guidId);
        await _farmersCollection.DeleteOneAsync(filter);
    }


    }
}