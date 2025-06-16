using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using PolyBucket.Service.Exceptions;
using PolyBucket.Service.Models;

namespace PolyBucket.Service.Services;

public class S3StorageService : BaseObjectStorageService
{
    private readonly IAmazonS3 _s3Client;
    public S3StorageService(ObjectStorageOptions options, ILogger<BaseObjectStorageService> logger, IAmazonS3 s3Client) : base(options, logger)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client), "Required S3 client is not provided.");
    }

    public override async Task UploadFileAsync(string bucketName, string objectName, Stream data, CancellationToken cancellationToken = default)
    {
        ValidateInput(bucketName, objectName);
        try
        {
            var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);

            if (_options.CreateBucketIfNotExists && !bucketExists)
            {
                await _s3Client.PutBucketAsync(bucketName, cancellationToken);
            }

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectName,
                InputStream = data,
                AutoCloseStream = true
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);

            _logger.LogInformation("File {ObjectName} uploaded to bucket {BucketName}.", objectName, bucketName);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {ObjectName} to bucket {BucketName}.", objectName, bucketName);

            throw new StorageException("File upload failed to S3.", ex);
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
                Key = objectName
            };
            using var resp = await _s3Client.GetObjectAsync(req, cancellationToken);
            await resp.ResponseStream.CopyToAsync(outputStream, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            _logger.LogWarning("File {ObjectName} not found in bucket {BucketName}.", objectName, bucketName);
            throw new StorageFileNotFoundException($"File '{objectName}' not found in bucket '{bucketName}'.", ex);
        }
        catch (AmazonS3Exception ex)
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
            await _s3Client.DeleteObjectAsync(bucketName, objectName, cancellationToken);
            _logger.LogInformation("File {ObjectName} deleted from bucket {BucketName}.", objectName, bucketName);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {ObjectName} from bucket {BucketName}.", objectName, bucketName);
            throw new StorageException("Error deleting file.", ex);
        }
    }
    public override async Task<IEnumerable<string>> ListFilesAsync(string bucketName, string prefix = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucketName));

        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix
            };

            var result = new List<string>();
            ListObjectsV2Response response;

            do
            {
                response = await _s3Client.ListObjectsV2Async(request, cancellationToken);
                result.AddRange(response.S3Objects.Select(o => o.Key));
                request.ContinuationToken = response.NextContinuationToken;
            } while ((bool)response.IsTruncated!);

            return result;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error listing files in bucket {BucketName} with prefix {Prefix}", bucketName, prefix);
            throw new StorageException($"AWS S3 list files failed: {ex.Message}", ex);
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
                Key = objectName
            };

            await _s3Client.GetObjectMetadataAsync(request, cancellationToken);

            return true;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NotFound")
        {
            _logger.LogInformation("File {ObjectName} does not exist in bucket {BucketName}.", objectName, bucketName);
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error checking file existence {ObjectName} in bucket {BucketName}", objectName, bucketName);
            throw new StorageException($"AWS S3 existence check failed: {ex.Message}", ex);
        }
    }
    public override async Task<string> GeneratePresignedUrl(string bucketName, string objectName, TimeSpan expiry)
    {
        ValidateInput(bucketName, objectName);

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = objectName,
                Expires = DateTime.UtcNow.Add(expiry),
                Verb = HttpVerb.GET
            };

            return await _s3Client.GetPreSignedURLAsync(request);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error generating presigned URL for {ObjectName} in bucket {BucketName}", objectName, bucketName);
            throw new StorageException($"AWS S3 presigned URL generation failed: {ex.Message}", ex);
        }
    }
    public override void Dispose()
    {
        _s3Client?.Dispose();
        base.Dispose();
    }
}
