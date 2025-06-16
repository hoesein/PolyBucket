using Microsoft.Extensions.Logging;
using OBS;
using OBS.Model;
using PolyBucket.Service.Exceptions;
using PolyBucket.Service.IServices;
using PolyBucket.Service.Models;

namespace PolyBucket.Service.Services;

public class ObsStorageService : BaseObjectStorageService
{
    private readonly IObsClient _obsClient;
    public ObsStorageService(ObjectStorageOptions options, ILogger<BaseObjectStorageService> logger, IObsClient obsClient) : base(options, logger)
    {
        _obsClient = obsClient ?? throw new ArgumentNullException(nameof(obsClient), "Required OBS client is not provided.");
    }

    public override async Task UploadFileAsync(string bucketName, string objectName, Stream data, CancellationToken cancellationToken = default)
    {
        ValidateInput(bucketName, objectName);
        try
        {
            if (_options.CreateBucketIfNotExists && !await BucketExist(bucketName))
            {
                await _obsClient.CreateBucketAsync(new CreateBucketRequest { BucketName = bucketName });
            }
            var req = new PutObjectRequest
            {
                BucketName = bucketName,
                ObjectKey = objectName,
                InputStream = data
            };
            await _obsClient.PutObjectAsync(req);
            _logger.LogInformation("File {ObjectName} uploaded to bucket {BucketName}.", objectName, bucketName);
        }
        catch (ObsException ex)
        {
            _logger.LogError(ex, "Error uploading file {ObjectName} to bucket {BucketName}.", objectName, bucketName);
            throw new StorageException("File upload failed to S3.", ex);
        }
    }
    private async Task<bool> BucketExist(string bucketName)
    {
        try
        {
            var bucket = await _obsClient.GetBucketMetaDataAsync(new GetBucketMetadataRequest
            {
                BucketName = bucketName
            });
            if (bucket is not null) return true;
            return false;
        }
        catch (ObsException)
        {
            return false;
        }
    }
    public override async Task DownloadFileAsync(string bucketName, string objectName, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ValidateInput(bucketName, objectName);
        try
        {
            var req = new GetObjectRequest
            {
                BucketName = bucketName,
                ObjectKey = objectName
            };

            var resp = await _obsClient.GetObjectAsync(req);
            //using var ms = new MemoryStream();
            await resp.OutputStream.CopyToAsync(outputStream, cancellationToken);
        }
        catch (ObsException ex) when (ex.ErrorCode == "NoSuchKey")
        {
            _logger.LogWarning("File {ObjectName} not found in bucket {BucketName}.", objectName, bucketName);
            throw new StorageFileNotFoundException($"File '{objectName}' not found in bucket '{bucketName}'.", ex);
        }
        catch (ObsException ex)
        {
            _logger.LogError(ex, "Error downloading file {ObjectName} from bucket {BucketName}.", objectName, bucketName);
            throw new StorageException("File download failed from S3.", ex);
        }
    }
    public override async Task DeleteFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        ValidateInput(bucketName, objectName);
        try
        {
            await _obsClient.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucketName, ObjectKey = objectName });
            _logger.LogInformation("File {ObjectName} deleted from bucket {BucketName}.", objectName, bucketName);
        }
        catch (ObsException ex)
        {
            _logger.LogError(ex, "Error deleting file {ObjectName} from bucket {BucketName}.", objectName, bucketName);
            throw new StorageException("Error deleting file.", ex);
        }
    }
    public override async Task<bool> FileExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        ValidateInput(bucketName, objectName);

        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                ObjectKey = objectName
            };

            await _obsClient.GetObjectMetadataAsync(request);

            return true;
        }
        catch (ObsException ex) when (ex.ErrorCode == "NotFound")
        {
            _logger.LogInformation("File {ObjectName} does not exist in bucket {BucketName}.", objectName, bucketName);
            return false;
        }
        catch (ObsException ex)
        {
            _logger.LogError(ex, "Huawei OBS error checking file existence {ObjectName} in bucket {BucketName}", objectName, bucketName);
            throw new StorageException($"Huawei OBS existence check failed: {ex.Message}", ex);
        }
    }
    public override async Task<string> GeneratePresignedUrl(string bucketName, string objectName, TimeSpan expiry)
    {
        ValidateInput(bucketName, objectName);
        try
        {
            var request = new CreateTemporarySignatureRequest
            {
                BucketName = bucketName,
                ObjectKey = objectName,
                Expires = expiry.Seconds,
            };
            var resp = await _obsClient.CreateTemporarySignatureAsync(request);

            _logger.LogInformation("Generated presigned URL for {ObjectName} in bucket {BucketName}: {@resp}", objectName, bucketName, resp);
            return resp.SignUrl;
        }
        catch (ObsException ex)
        {
            _logger.LogError(ex, "Huawei OBS error generating presigned URL for {ObjectName} in bucket {BucketName}", objectName, bucketName);
            throw new StorageException($"Huawei OBS presigned URL generation failed: {ex.Message}", ex);
        }
    }
    public override async Task<IEnumerable<string>> ListFilesAsync(string bucketName, string prefix = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucketName));
        try
        {
            var req = new ListObjectsRequest
            {
                BucketName = bucketName,
                Prefix = prefix
            };
            var result = new List<string>();
            var response = await _obsClient.ListObjectsAsync(req);
            foreach (var obj in response.ObsObjects)
            {
                result.Add(obj.ObjectKey);
            }
            return result;
        }
        catch (ObsException ex)
        {
            _logger.LogError(ex, "Huawei OBS error listing files in bucket {BucketName} with prefix {Prefix}", bucketName, prefix);
            throw new StorageException($"Huawei OBS list files failed: {ex.Message}", ex);
        }
    }
}
