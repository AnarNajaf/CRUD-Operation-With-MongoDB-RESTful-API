using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using iTarlaMapBackend.Models;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<Farm?> GetAsync(string id)
        {
            if(!Guid.TryParse(id, out Guid guiId))
            throw new ArgumentException("Invalid GUID format", nameof(id));
            return await _farmCollection.Find(f => f.Id == guiId).FirstOrDefaultAsync();
        }
        public async Task<List<Farm>> GetAsync()
        {
            return await _farmCollection.Find(_ => true).ToListAsync();
        }
        public async Task CreateAsync(Farm farm)
        {
            if (farm.Id == Guid.Empty)
                farm.Id = Guid.NewGuid();
            await _farmCollection.InsertOneAsync(farm);
        }
        public async Task UpdateAsync(string farmId, Farm updatedFarm)
        {
            if(!Guid.TryParse(farmId, out Guid guidId))
            throw new ArgumentException("Invalid GUID format", nameof(farmId));
            updatedFarm.Id = guidId;
            var filter = Builders<Farm>.Filter.Eq(f=>f.Id, guidId);
            await _farmCollection.ReplaceOneAsync(filter, updatedFarm);
        }
        public async Task RemoveAsync(string id)
        {
            if(!Guid.TryParse(id, out Guid guidId))
            throw new ArgumentException("Invalid GUID format", nameof(id));
            var filter = Builders<Farm>.Filter.Eq(f => f.Id, guidId);
            await _farmCollection.DeleteOneAsync(filter);
        }
    }
}