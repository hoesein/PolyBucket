namespace PolyBucket.Service.Exceptions;
public class StorageFileNotFoundException : Exception
{
    public StorageFileNotFoundException(string message) : base(message) { }
    public StorageFileNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}