using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Moq;
using PolyBucket.Service.Exceptions;
using PolyBucket.Service.Models;
using PolyBucket.Service.Services;
using System.Text;

namespace PolyBucket.Test;

public class S3StorageServiceTests
{
    private readonly Mock<IAmazonS3> _mockS3;
    private readonly S3StorageService _s3StorageService;
    private readonly Mock<ILogger<S3StorageService>> _mockLogger;
    private readonly ObjectStorageOptions _options;

    public S3StorageServiceTests()
    {
        _mockS3 = new Mock<IAmazonS3>();

        _mockLogger = new Mock<ILogger<S3StorageService>>();

        // Default options for tests
        _options = new ObjectStorageOptions
        {
            Endpoint = "http://localhost:9000",
            Region = "us-east-1",
            AccessKey = "test-access-key",
            SecretKey = "test-secret-key",
            TimeoutSeconds = 30,
            CreateBucketIfNotExists = true // Default to true for relevant tests
        };

        _s3StorageService = new S3StorageService(_options, _mockLogger.Object, _mockS3.Object);
    }

    [Fact]
    public async Task UploadFileAsync_should_upload_to_exists_bucket()
    {
        // arrange
        var bucketName = "new-bucket";
        var objectName = "test-file.txt";
        var fileContent = "This is a test file content.";
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // create mock bucket
        _mockS3.Setup(x => x.PutBucketAsync(bucketName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutBucketResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        // put mock object
        _mockS3.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new PutObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        // act
        await _s3StorageService.UploadFileAsync(bucketName, objectName, memoryStream);

        // assert
        //_mockS3.Verify(x => x.PutBucketAsync(bucketName, It.IsAny<CancellationToken>()), Times.Once, "Should attempt to create the bucket.");
        _mockS3.Verify(x => x.PutObjectAsync(It.Is<PutObjectRequest>(req =>
            req.BucketName == bucketName &&
            req.Key == objectName &&
            req.InputStream == memoryStream
        ), It.IsAny<CancellationToken>()), Times.Once, "Should upload the file to the bucket.");
    }
    [Fact]
    public async Task UploadFileAsync_when_s3_throw_exception_should_throw_storage_exception()
    {
        // Arrange
        var bucketName = "new-bucket";
        var objectName = "new-file.txt";
        var dataStream = new MemoryStream();

        _mockS3.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("S3 Error"));

        // Act
        var act = async () => await _s3StorageService.UploadFileAsync(bucketName, objectName, dataStream);

        // Assert
        StorageException ex = await Task.Run(() => Assert.ThrowsAsync<StorageException>(act));
    }
    [Fact]
    public async Task DownloadFileAsync_when_file_exists_should_copy_stream()
    {
        // arrange
        var bucketName = "existing-bucket";
        var objectName = "existing-file.txt";
        var expectedContent = "This is the content of the file.";
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedContent));
        var outputStream = new MemoryStream();

        var getObjectResponse = new GetObjectResponse
        {
            ResponseStream = responseStream,
            HttpStatusCode = System.Net.HttpStatusCode.OK
        };

        _mockS3.Setup(x => x.GetObjectAsync(It.Is<GetObjectRequest>(req => req.BucketName == bucketName && req.Key == objectName), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getObjectResponse);

        // act
        await _s3StorageService.DownloadFileAsync(bucketName, objectName, outputStream);

        // assert
        outputStream.Seek(0, SeekOrigin.Begin);
        var actualContent = new StreamReader(outputStream).ReadToEnd();
        Assert.Equal(expectedContent, actualContent);
    }
    [Fact]
    public async Task DownloadFileAsync_when_file_does_not_exist_should_throw_storage_file_not_found_exception()
    {
        // arrange
        var bucketName = "existing-bucket";
        var objectName = "existing-file.txt";

        _mockS3.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Not Found") { ErrorCode = "NoSuchKey" });

        // act
        var act = async () => await _s3StorageService.DownloadFileAsync(bucketName, objectName, new MemoryStream());

        // assert
        await Assert.ThrowsAsync<StorageFileNotFoundException>(act);
    }
    [Fact]
    public async Task DownloadFileAsync_when_s3_throw_other_exception_should_throw_stroage_exception()
    {
        // arrange
        var bucketName = "existing-bucket";
        var objectName = "existing-file.txt";

        _mockS3.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Access Denied") { ErrorCode = "AccessDenied" });

        // act
        var act = async () => await _s3StorageService.DownloadFileAsync(bucketName, objectName, new MemoryStream());

        // assert
        await Assert.ThrowsAsync<StorageException>(act);
    }
    [Fact]
    public async Task DeleteFileAsync_when_called_should_invoke_s3_delete_object()
    {
        // arrange
        var bucketName = "delete-bucket";
        var objectName = "delete-file.txt";

        _mockS3.Setup(x => x.DeleteObjectAsync(bucketName, objectName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse());

        // act
        await _s3StorageService.DeleteFileAsync(bucketName, objectName);

        // assert
        _mockS3.Verify(x => x.DeleteObjectAsync(bucketName, objectName, It.IsAny<CancellationToken>()), Times.Once, "Should delete the file from the bucket.");
    }
    [Fact]
    public async Task DeleteFileAsync_when_s3_throw_exception_should_throw_storage_exception()
    {
        // arrange
        var bucketName = "delete-bucket";
        var objectName = "delete-file.txt";

        _mockS3.Setup(x => x.DeleteObjectAsync(bucketName, objectName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("S3 Error"));

        // act
        var act = async () => await _s3StorageService.DeleteFileAsync(bucketName, objectName);

        // assert
        await Assert.ThrowsAsync<StorageException>(act);
    }
}