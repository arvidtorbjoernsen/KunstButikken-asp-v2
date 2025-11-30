using KunstButikken.ArtService.Domain.Interfaces;

namespace KunstButikken.ArtService.Tests.Fakes;

internal sealed class FakeBlobStorage : IBlobStorage
{
    public Uri? LastUploaded { get; private set; }
    public string? LastBlobName { get; private set; }

    public Task<string> UploadAsync(string blobName, Stream content, string? contentType, CancellationToken cancellationToken = default)
    {
        LastBlobName = blobName;
        LastUploaded = new Uri($"https://storage/{blobName}");
        return Task.FromResult(LastUploaded.ToString());
    }
}
