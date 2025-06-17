using Microsoft.Extensions.Logging;
using Moq;
using PolyBucket.Service.Models;
using PolyBucket.Service.Services;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Threading.Tasks;

namespace PolyBucket.Test;

public class LocalStorageServiceTests
{
    private readonly LocalStorageService _localStorageService;
    private readonly ObjectStorageOptions _options;
    private readonly Mock<ILogger<LocalStorageService>> _mockLogger;
    private readonly MockFileSystem _mockFileSystem;

    public LocalStorageServiceTests()
    {
        _mockLogger = new Mock<ILogger<LocalStorageService>>();
        // Default options for tests
        _options = new ObjectStorageOptions
        {
            LocalStoragePath = @"D:\Temp\PolyBucketTestStorage", // Use a temporary path for testing
        };

        // Mock the file system
        _mockFileSystem = new MockFileSystem();

        _localStorageService = new LocalStorageService(_options, _mockLogger.Object, _mockFileSystem);
    }

    [Fact()]
    public async Task UploadFileAsyncTest()
    {
        // Arrange
        var bucketName = "new-bucket";
        var objectName = "test-file.txt";
        var fileContent = "This is a test file content.";
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var expectedPath = _mockFileSystem.Path.Combine(_options.LocalStoragePath, bucketName, objectName);

        // Act
        await _localStorageService.UploadFileAsync(bucketName, objectName, memoryStream);

        // Assert
        var filePath = _mockFileSystem.Path.Combine(_options.LocalStoragePath, bucketName, objectName);
        Assert.True(_mockFileSystem.File.Exists(filePath), "File should be uploaded successfully.");
    }

    [Fact()]
    public async Task DeleteFileAsyncTest()
    {
        // Arrange
        var bucketName = "delete-bucket";
        var objectName = "delete-file.txt";
        var bucketPath = _mockFileSystem.Path.Combine(_options.LocalStoragePath, bucketName);
        var filePath = _mockFileSystem.Path.Combine(bucketPath, objectName);

        _mockFileSystem.AddDirectory(bucketPath);
        _mockFileSystem.AddFile(filePath, new MockFileData("This is a file to be deleted."));

        Assert.True(_mockFileSystem.File.Exists(filePath), "File should be exist before delete method call.");

        // Act
        await _localStorageService.DeleteFileAsync(bucketName, objectName);

        // Assert
        Assert.False(_mockFileSystem.File.Exists(filePath), "File should be delete after delete method call.");
    }

    [Fact()]
    public async Task DownloadFileAsyncTest()
    {
        // Arrange
        var bucketName = "download-bucket";
        var objectName = "download-file.txt";
        var bucketPath = _mockFileSystem.Path.Combine(_options.LocalStoragePath, bucketName);
        var filePath = _mockFileSystem.Path.Combine(bucketPath, objectName);

        _mockFileSystem.AddDirectory(bucketPath);
        _mockFileSystem.AddFile(filePath, new MockFileData("This is a file to be downloaded."));

        // Ensure the file exists before downloading
        Assert.True(_mockFileSystem.File.Exists(filePath), "File should exist before download method call.");

        var outputStream = new MemoryStream();

        // Act
        await _localStorageService.DownloadFileAsync(bucketName, objectName, outputStream);

        // Assert
        outputStream.Seek(0, SeekOrigin.Begin);
        var result = new StreamReader(outputStream).ReadToEnd();
        Assert.Equal("This is a file to be downloaded.", result);
    }

    [Fact()]
    public async Task FileExistsAsyncTest()
    {
        // Arrange
        var bucketName = "exist-bucket";
        var objectName = "exist-file.txt";
        var bucketPath = _mockFileSystem.Path.Combine(_options.LocalStoragePath, bucketName);
        var filePath = _mockFileSystem.Path.Combine(bucketPath, objectName);

        _mockFileSystem.AddDirectory(bucketPath);
        _mockFileSystem.AddFile(filePath, new MockFileData("This is a file to be exist test."));

        // Act
        await _localStorageService.FileExistsAsync(bucketName, objectName);

        // Assert
        Assert.True(_mockFileSystem.File.Exists(filePath), "File should exist in directory");
    }

    [Fact()]
    public void GeneratePresignedUrlTest()
    {
        // Arrange
        var bucketName = "presigned-bucket";
        var objectName = "presigned-file.txt";
        var expiration = TimeSpan.FromHours(1);
        // Act
        var url = _localStorageService.GeneratePresignedUrl(bucketName, objectName, expiration);
        // Assert
        Assert.NotNull(url);
    }

    [Fact()]
    public async Task ListFilesAsyncTest()
    {
        // Arrange
        var bucketName = "list-bucket";
        var objectName1 = "file1.txt";
        var objectName2 = "file2.txt";
        var bucketPath = _mockFileSystem.Path.Combine(_options.LocalStoragePath, bucketName);
        
        _mockFileSystem.AddDirectory(bucketPath);
        _mockFileSystem.AddFile(_mockFileSystem.Path.Combine(bucketPath, objectName1), new MockFileData("Content of file 1"));
        _mockFileSystem.AddFile(_mockFileSystem.Path.Combine(bucketPath, objectName2), new MockFileData("Content of file 2"));
        // Act
        var files = await _localStorageService.ListFilesAsync(bucketName);
        // Assert
        Assert.NotNull(files);
        Assert.Equal(2, files.ToList().Count);
        Assert.Contains(objectName1, files);
        Assert.Contains(objectName2, files);
    }
}