namespace PolyBucket.Service.IServices;

public interface IObsClient : IDisposable
{
    Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request);
    Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request);
    Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request);
    Task<GetObjectMetadataResponse> GetObjectMetadataAsync(GetObjectMetadataRequest request);
    Task<ListObjectsResponse> ListObjectsAsync(ListObjectsRequest request);
    Task<GetBucketMetadataResponse> GetBucketMetaDataAsync(GetBucketMetadataRequest request);
    Task<CreateBucketResponse> CreateBucketAsync(CreateBucketRequest request);
    Task<CreateTemporarySignatureResponse> CreateTemporarySignatureAsync(CreateTemporarySignatureRequest request);
}