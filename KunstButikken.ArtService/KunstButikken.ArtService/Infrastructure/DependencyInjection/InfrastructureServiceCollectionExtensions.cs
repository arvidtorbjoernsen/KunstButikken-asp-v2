using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using KunstButikken.ArtService.Infrastructure.Data;
using KunstButikken.ArtService.Domain.Interfaces;
using KunstButikken.ArtService.Infrastructure.Repositories;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using KunstButikken.ArtService.Infrastructure.Storage;

namespace KunstButikken.ArtService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default")
                 ?? configuration.GetConnectionString("artdb");

        if (!string.IsNullOrWhiteSpace(cs) && !cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            // Register the concrete Infrastructure.Data.ArtDbContext so components that request
            // KunstButikken.ArtService.Infrastructure.Data.ArtDbContext will be satisfied. Use the same
            // migrations assembly as the concrete type.
            services.AddDbContext<ArtDbContext>(options =>
            {
                options.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(ArtDbContext).Assembly.FullName));
            });
        }
        else
        {
            services.AddDbContext<ArtDbContext>(options => options.UseInMemoryDatabase("art_inmemory"));
        }

        services.AddScoped<IArtRepository, ArtRepository>();

        var blobConnection = configuration["AzureBlob:ConnectionString"] ?? configuration.GetConnectionString("artimages");
        if (!string.IsNullOrWhiteSpace(blobConnection))
        {
            services.AddSingleton(_ => new BlobServiceClient(blobConnection));
            services.AddSingleton<IBlobStorage, BlobStorage>();
        }
        else
        {
            services.AddSingleton<IBlobStorage, NullBlobStorage>();
        }
        return services;
    }
}
