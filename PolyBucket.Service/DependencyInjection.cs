namespace PolyBucket.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceDependencies(this IServiceCollection services, IConfiguration config)
    {
        return services; 
    }
}
