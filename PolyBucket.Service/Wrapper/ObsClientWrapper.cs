using OBS;
using OBS.Model;
using PolyBucket.Service.IServices;

namespace PolyBucket.Service.Wrapper;

public class ObsClientWrapper : IObsClient
{
    private readonly ObsClient _client;
    public ObsClientWrapper(string accessKey, string secretKey, string endpoint)
    {
        var config = new ObsConfig
        {
            Endpoint = endpoint
        };
        _client = new ObsClient(accessKey, secretKey, config);
    }
    public async Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request) => await Task.Run(() => _client.PutObject(request));
    public async Task<CreateBucketResponse> CreateBucketAsync(CreateBucketRequest request) => await Task.Run(() => _client.CreateBucket(request));
    public async Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request) => await Task.Run(() => _client.DeleteObject(request));
    public async Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request) => await Task.Run(() => _client.GetObject(request));
    public async Task<GetObjectMetadataResponse> GetObjectMetadataAsync(GetObjectMetadataRequest request) => await Task.Run(() => _client.GetObjectMetadata(request));
    public async Task<ListObjectsResponse> ListObjectsAsync(ListObjectsRequest request) => await Task.Run(() => _client.ListObjects(request));
    public async Task<GetBucketMetadataResponse> GetBucketMetaDataAsync(GetBucketMetadataRequest request) => await Task.Run(() => _client.GetBucketMetadata(request));
    public async Task<CreateTemporarySignatureResponse> CreateTemporarySignatureAsync(CreateTemporarySignatureRequest request) => await Task.Run(() => _client.CreateTemporarySignature(request));
    public void Dispose()
    {
        (_client as IDisposable)?.Dispose();
    }
}
