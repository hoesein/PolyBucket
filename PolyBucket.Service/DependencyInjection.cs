using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PolyBucket.Service.IServices;
using PolyBucket.Service.Models;
using PolyBucket.Service.Services;
using PolyBucket.Service.Wrapper;

namespace PolyBucket.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceDependencies(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ObjectStorageOptions>(config.GetSection("ObjectStorage"));

        services.AddSingleton<IAmazonS3>(opt =>
        {
            var options = opt.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
            var loggerFactory = opt.GetRequiredService<ILoggerFactory>();
            var config = new AmazonS3Config
            {
                ServiceURL = options.Endpoint,
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region ?? "us-east-1"),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                MaxErrorRetry = 3,
                ForcePathStyle = true // Use path-style URLs for S3 compatibility
            };
            return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        });

        services.AddSingleton<IObsClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
            return new ObsClientWrapper(options.AccessKey, options.SecretKey, options.Endpoint);
        });

        services.AddTransient<IObjectStorageService>(provider =>
        {
            var opt = provider.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var obsClient = provider.GetRequiredService<IObsClient>();
            var s3Client = provider.GetService<IAmazonS3>();

            return opt.Provider switch
            {
                StorageProvider.AwsS3 => new S3StorageService(opt, loggerFactory.CreateLogger<S3StorageService>(), s3Client!),
                StorageProvider.HuaweiObs => new ObsStorageService(opt, loggerFactory.CreateLogger<ObsStorageService>(), obsClient),
                //StorageProvider.Local => new LocalStorageService(opt, loggerFactory.CreateLogger<LocalStorageService>()),
                _ => throw new NotSupportedException($"Unsupported storage type: {opt.Provider}")
            };
        });

        return services;
    }
}
