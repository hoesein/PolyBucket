namespace PolyBucket.Service.IServices;

public interface IObjectStorageService : IDisposable
{
    Task UploadFileAsync(string bucketName, string objectName, Stream data, CancellationToken cancellationToken = default);
    Task DownloadFileAsync(string bucketName, string objectName, Stream outputStream, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> ListFilesAsync(string bucketName, string? prefix = null, CancellationToken cancellationToken = default);
    Task<string> GeneratePresignedUrl(string bucketName, string objectName, TimeSpan expiry);
}