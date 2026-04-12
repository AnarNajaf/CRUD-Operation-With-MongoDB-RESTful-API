namespace iTarlaMapBackend.Models
{
    public class iTarlaDbConnection
    {
        public string ConnectionString { get; set; }= null!;
        public string DatabaseName { get; set; }=null!;
        public string SensorsCollectionName { get; set; }=null!;
        public string MotorsCollectionName { get; set; }=null!;
        public string FarmsCollectionName { get; set; }=null!;
        public string FarmersCollectionName { get; set; }=null!;
    }
}