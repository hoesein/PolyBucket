using Microsoft.Extensions.Logging;
using PolyBucket.Service.Exceptions;
using PolyBucket.Service.Models;

namespace PolyBucket.Service.Services;

public class LocalStorageService : BaseObjectStorageService
{
    public LocalStorageService(ObjectStorageOptions options, ILogger<BaseObjectStorageService> logger) : base(options, logger)
    {
        if (!Directory.Exists(options.LocalStoragePath))
        {
            Directory.CreateDirectory(options.LocalStoragePath);
        }
    }

    public override async Task UploadFileAsync(string bucketName, string objectName, Stream data, CancellationToken cancellationToken = default)
    {
        ValidateInput(bucketName, objectName);
        try
        {
            var bucketPath = Path.Combine(_options.LocalStoragePath, bucketName);
            if (!Directory.Exists(bucketPath))
            {
                Directory.CreateDirectory(bucketPath);
            }
            var filePath = Path.Combine(bucketPath, objectName);
            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await data.CopyToAsync(fileStream, cancellationToken);
            }
            _logger.LogInformation("File {ObjectName} uploaded to bucket {BucketName}.", objectName, bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorage error uploading file {ObjectName} to bucket {BucketName}.", objectName, bucketName);
            throw new StorageException("LocalStorage upload failed.", ex);
        }
    }

    public override async Task DeleteFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        ValidateInput(bucketName, objectName);
        try
        {
            var filePath = Path.Combine(_options.LocalStoragePath, bucketName, objectName);
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath), cancellationToken);
                _logger.LogInformation("File {ObjectName} deleted from bucket {BucketName}.", objectName, bucketName);
            }
            else
            {
                _logger.LogWarning("File {ObjectName} not found in bucket {BucketName}.", objectName, bucketName);
                throw new StorageFileNotFoundException($"File '{objectName}' not found in bucket '{bucketName}'.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorage error deleting file {ObjectName} from bucket {BucketName}.", objectName, bucketName);
            throw new StorageException("LocalStorage delete failed.", ex);
        }
    }

    public override Task DownloadFileAsync(string bucketName, string objectName, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ValidateInput(bucketName, objectName);
        try
        {
            var filePath = Path.Combine(_options.LocalStoragePath, bucketName, objectName);
            if (File.Exists(filePath))
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return fileStream.CopyToAsync(outputStream, cancellationToken);
            }
            else
            {
                _logger.LogWarning("File {ObjectName} not found in bucket {BucketName}.", objectName, bucketName);
                throw new StorageFileNotFoundException($"File '{objectName}' not found in bucket '{bucketName}'.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorage error downloading file {ObjectName} from bucket {BucketName}.", objectName, bucketName);
            throw new StorageException("LocalStorage download failed.", ex);
        }
    }

    public override Task<bool> FileExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        ValidateInput(bucketName, objectName);
        try
        {
            var filePath = Path.Combine(_options.LocalStoragePath, bucketName, objectName);
            return Task.FromResult(File.Exists(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorage error checking existence of file {ObjectName} in bucket {BucketName}.", objectName, bucketName);
            throw new StorageException("LocalStorage file existence check failed.", ex);
        }
    }

    public override async Task<string> GeneratePresignedUrl(string bucketName, string objectName, TimeSpan expiry)
    {
        ValidateInput(bucketName, objectName);
        var filepath = Path.Combine(_options.LocalStoragePath, bucketName, objectName);
        if (!File.Exists(filepath))
        {
            throw new StorageFileNotFoundException($"File '{objectName}' not found in bucket '{bucketName}'.");
        }
        return await Task.Run(() => filepath); // Local storage does not support presigned URLs, return the file path instead
    }

    public override Task<IEnumerable<string>> ListFilesAsync(string bucketName, string prefix = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucketName));
        }
        try
        {
            var bucketPath = Path.Combine(_options.LocalStoragePath, bucketName);
            if (!Directory.Exists(bucketPath))
            {
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
            }
            var files = Directory.GetFiles(bucketPath, prefix == null ? "*" : $"{prefix}*")
                                 .Select(Path.GetFileName)
                                 .ToList();
            _logger.LogInformation("Listed {FileCount} files in bucket {BucketName}.", files.Count, bucketName);
            return Task.FromResult<IEnumerable<string>>(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorage error listing files in bucket {BucketName}.", bucketName);
            throw new StorageException("LocalStorage list files failed.", ex);
        }
    }
}
