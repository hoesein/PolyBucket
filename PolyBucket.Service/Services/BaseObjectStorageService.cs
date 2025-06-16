using Microsoft.Extensions.Logging;
using PolyBucket.Service.IServices;
using PolyBucket.Service.Models;

namespace PolyBucket.Service.Services;

public abstract class BaseObjectStorageService : IObjectStorageService
{
    protected readonly ObjectStorageOptions _options;
    protected readonly ILogger<BaseObjectStorageService> _logger;

    protected BaseObjectStorageService(ObjectStorageOptions options, ILogger<BaseObjectStorageService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public abstract Task UploadFileAsync(string bucketName, string objectName, Stream data, CancellationToken cancellationToken = default);
    public abstract Task DeleteFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
    public abstract Task DownloadFileAsync(string bucketName, string objectName, Stream outputStream, CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<string>> ListFilesAsync(string bucketName, string? prefix = null, CancellationToken cancellationToken = default);
    public abstract Task<bool> FileExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
    public abstract Task<string> GeneratePresignedUrl(string bucketName, string objectName, TimeSpan expiry);
    public virtual void Dispose()
    {
        // Base dispose - override in derived classes if needed
        GC.SuppressFinalize(this);
    }
    protected virtual void ValidateInput(string bucketName, string objectName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucketName));

        if (string.IsNullOrWhiteSpace(objectName))
            throw new ArgumentException("Object name cannot be null or empty", nameof(objectName));
    }
}
