using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;

namespace iTarlaMapBackend.Services
{
    public class FirebaseService
    {
        private readonly FirestoreDb _db;

        public FirebaseService(IConfiguration config)
        {
            var projectId = config["Firebase:ProjectId"];
            _db = FirestoreDb.Create(projectId);
        }

        public async Task SetMotorActiveAsync(string deviceCode, bool isActive)
        {
            var docRef = _db.Collection("Motors").Document(deviceCode);
            var data = new Dictionary<string, object>
            {
                { "isActive", isActive },
                { "updatedAt", Timestamp.GetCurrentTimestamp() }
            };

            // Reset the running-hours counter when motor is turned off
            // so the "running Xh" alert doesn't fire with stale data on next start
            if (!isActive)
                data["activeTimeHours"] = 0;

            await docRef.SetAsync(data, SetOptions.MergeAll);
        }

        // Returns when the sensor last pushed data, or null if unknown/missing
        public async Task<DateTime?> GetSensorLastUpdateAsync(string deviceCode)
        {
            var snapshot = await _db.Collection("Sensors").Document(deviceCode).GetSnapshotAsync();
            if (!snapshot.Exists) return null;
            if (snapshot.TryGetValue<Timestamp>("updatedAt", out var ts))
                return ts.ToDateTime();
            return null;
        }

        // Returns current soil moisture (0-100), or null if field missing
        public async Task<(double? moisture, DateTime? updatedAt)> GetSensorMoistureAsync(string deviceCode)
        {
            var snapshot = await _db.Collection("Sensors").Document(deviceCode).GetSnapshotAsync();
            if (!snapshot.Exists) return (null, null);

            double? moisture = null;
            DateTime? updatedAt = null;

            // Firestore stores manually-entered integers as long, not double — try both
            if (snapshot.TryGetValue<double>("soilMoisture", out var mDouble))
                moisture = mDouble;
            else if (snapshot.TryGetValue<long>("soilMoisture", out var mLong))
                moisture = (double)mLong;

            if (snapshot.TryGetValue<Timestamp>("updatedAt", out var ts))
                updatedAt = ts.ToDateTime();

            return (moisture, updatedAt);
        }
    }
}