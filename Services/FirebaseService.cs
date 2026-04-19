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
            await docRef.SetAsync(new Dictionary<string, object>
            {
                { "isActive", isActive },
                { "updatedAt", Timestamp.GetCurrentTimestamp() }
            }, SetOptions.MergeAll);
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
    }
}