using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using KunstButikken.ArtService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KunstButikken.ArtService.Infrastructure.Storage;

internal sealed class BlobStorage : IBlobStorage
{
    private readonly BlobContainerClient _container;
    private readonly string? _publicUrlOverride;

    public BlobStorage(BlobServiceClient blobServiceClient, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(blobServiceClient);
        ArgumentNullException.ThrowIfNull(configuration);

        var containerName = configuration["AzureBlob:Container"] ?? "images";
        _publicUrlOverride = configuration["AzureBlob:PublicUrl"];

        Console.WriteLine($"[DEBUG] Using blob service, Container: {containerName}");
        if (!string.IsNullOrEmpty(_publicUrlOverride))
        {
            Console.WriteLine($"[DEBUG] Public URL override configured: {_publicUrlOverride}");
        }

        _container = blobServiceClient.GetBlobContainerClient(containerName);
        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> UploadAsync(string blobName, Stream data, string? contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blobName);
        ArgumentNullException.ThrowIfNull(data);

        var blobClient = _container.GetBlobClient(blobName);
        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };
        await blobClient.UploadAsync(data, new BlobUploadOptions
            {
                HttpHeaders = headers
            }, cancellationToken)
            .ConfigureAwait(false);

        // Use public URL override if configured (for Azurite with dynamic ports)
        if (!string.IsNullOrEmpty(_publicUrlOverride))
        {
            var containerName = _container.Name;
            var accountName = _container.AccountName;
            return $"{_publicUrlOverride}/{accountName}/{containerName}/{blobName}";
        }

        return blobClient.Uri.ToString();
    }
}
