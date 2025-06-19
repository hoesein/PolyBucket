[![PolyBucket CI](https://github.com/hoesein/PolyBucket/actions/workflows/poly-bucket-ci.yml/badge.svg)](https://github.com/hoesein/PolyBucket/actions/workflows/poly-bucket-ci.yml)

# Object Storage Service Library
A generic C# library for interacting with multiple cloud storage providers with a unified API interface.

## Features
-   **Multi-Provider Support**: Upload, download, and manage files across various cloud storage services
-   **Consistent API**: Same interface for all supported providers    
-   **Extensible Architecture**: Easily add support for new storage providers
-   **Dependency Injection Ready**: Built for modern .NET applications
-   **Comprehensive Testing**: Unit and integration test coverage

## Supported Providers

|Provider | Status | Notes |
|--------- | -------- | ------- |
| AWS S3 | :heavy_check_mark: | Full support |
| Huawei OBS | :heavy_check_mark: | Full support |
| DigitalOcean | :heavy_check_mark: | S3-compatible |
| MinIO | :heavy_check_mark: | S3-compatible |
| Local Storage | :heavy_check_mark: | For development/testing |
| Azure Blob | :hourglass: | Planned for next release |
| Google Cloud | :hourglass: | Planned for next release |

## Getting Started

### Installation

```bash
dotnet add package ObjectStorageService 
```

### Basic Usage

``` csharp
// Configure in your Startup.cs
services.AddObjectStorage(Configuration);

// Inject and use in your services
public class MyService
{
    private readonly IObjectStorageService _storage;

    public MyService(IObjectStorageService storage)
    {
        _storage = storage;
    }

    public async Task UploadFileAsync(string bucket, string key, Stream data)
    {
        await _storage.UploadFileAsync(bucket, key, data);
    }
}
```

### Configuration (appsettings.json)

```json
{
  "ObjectStorage": {
    "Provider": "AwsS3", // or "HuaweiObs", "DigitalOcean", "MinIO", "Local"
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key",
    "Endpoint": "https://your-endpoint.com",
    "Region": "us-east-1",
    "LocalStoragePath": "/path/to/local/storage" // For local provider
  }
}
```

## Adding New Storage Providers

### Implementing a New Provider

1.  Create a new class that implements  `IObjectStorageService`
    
2.  Add your provider to the  `StorageProvider`  enum
    
3.  Update the factory/service registration
    

Example for MinIO (S3-compatible):

```csharp
public class MinIOStorageService : BaseObjectStorageService
{
    private readonly IAmazonS3 _s3Client;

    public MinIOStorageService(ObjectStorageOptions options, ILogger<MinIOStorageService> logger)
        : base(options, logger)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = true, // Important for MinIO
            Timeout = TimeSpan.FromSeconds(60),
            MaxErrorRetry = 3
        };

        _s3Client = new AmazonS3Client(options.AccessKey, options.SecretKey, config);
    }

    // Implement required methods using _s3Client
    // ...
}
```

### Registering the New Provider

Update the factory method:

```csharp

services.AddTransient<IObjectStorageService>(provider =>
{
    var options = provider.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
    return options.Provider switch
    {
        StorageProvider.AwsS3 => new AwsS3StorageService(options, logger),
        StorageProvider.MinIO => new MinIOStorageService(options, logger),
        // ... other providers
        _ => throw new InvalidOperationException($"Unsupported provider: {options.Provider}")
    };
});
```

## Contributing

We welcome contributions! Here's how you can help:
### How to Contribute
1.  Fork the repository    
2.  Create a feature branch (`git checkout -b feature/your-feature`)    
3.  Commit your changes (`git commit -m 'Add some feature'`)    
4.  Push to the branch (`git push origin feature/your-feature`)    
5.  Open a Pull Request    

### Contribution Guidelines

-   Follow existing code style and patterns    
-   Include unit tests for new features    
-   Update documentation when adding new features    
-   Keep PRs focused on a single feature/bugfix    
-   Use descriptive commit messages    

### Feature Requests

We're particularly interested in adding support for:
-   Azure Blob Storage    
-   Google Cloud Storage    
-   Backblaze B2    
-   Alibaba Cloud OSS    
-   Advanced file metadata support    
-   Multi-part upload/download    
-   Client-side encryption    

## License

This project is licensed under the MIT License - see the  [LICENSE](https://license/)  file for details.

MIT License gives you the freedom to:
-   Use the software for any purpose    
-   Modify the software to suit your needs    
-   Distribute the original or modified software    
-   Use the software in private or commercial projects    

## Roadmap

### v1.1 (Next Release)
-   Add Azure Blob Storage support    
-   Improve error handling and retry policies    
-   Add file metadata support    

### v1.2

-   Google Cloud Storage integration    
-   Client-side encryption    
-   Multi-part upload support    

### Future

-   Storage analytics    
-   Automatic provider failover    
-   Browser upload integration
