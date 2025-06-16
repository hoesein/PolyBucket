namespace PolyBucket.Service.Models;

public class ObjectStorageOptions
{
    public StorageProvider Provider { get; set; } = StorageProvider.Local;
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string Endpoint { get; set; }
    public string Region { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public string LocalStoragePath { get; set; } = Path.Combine(Environment.CurrentDirectory, "storage");
    public bool CreateBucketIfNotExists { get; set; } = true;
}

public enum StorageProvider
{
    AwsS3,
    HuaweiObs,
    DigitalOcean,
    Local
}
