using Amazon;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yoxel.Storage.Core.Abstractions;
using Yoxel.Storage.Core.Services;
using Yoxel.Storage.Infrastructure.Events;
using Yoxel.Storage.Infrastructure.Persistence;
using Yoxel.Storage.Infrastructure.Storage;

namespace Yoxel.Storage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStorageInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Metadata DB
        services.AddDbContext<StorageDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("StorageDb")));
        services.AddScoped<IFileMetadataRepository, EfFileMetadataRepository>();

        // Application service
        services.AddScoped<IStorageService, StorageService>();

        // Event publisher (no-op default; replace with Kafka/RabbitMQ in prod)
        services.AddSingleton<IStorageEventPublisher, NoopStorageEventPublisher>();

        // Pluggable file backend
        var backend = configuration["Storage:Backend"] ?? "Local";
        if (string.Equals(backend, "S3", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<S3StorageOptions>(configuration.GetSection("Storage:S3"));
            services.AddSingleton<IAmazonS3>(_ =>
            {
                var section = configuration.GetSection("Storage:S3");
                var serviceUrl = section["ServiceUrl"];
                var region = section["Region"];
                var forcePathStyle = bool.TryParse(section["ForcePathStyle"], out var fps) && fps;

                var config = new AmazonS3Config { ForcePathStyle = forcePathStyle };
                if (!string.IsNullOrWhiteSpace(serviceUrl))
                {
                    config.ServiceURL = serviceUrl;
                }
                if (!string.IsNullOrWhiteSpace(region))
                {
                    config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
                }

                return new AmazonS3Client(config);
            });
            services.AddSingleton<IFileStorage, S3FileStorage>();
        }
        else
        {
            services.Configure<LocalFileStorageOptions>(configuration.GetSection("Storage:Local"));
            services.AddSingleton<IFileStorage, LocalFileStorage>();
        }

        return services;
    }
}
