namespace KunstButikken.ArtService.Domain.Interfaces;

public interface IBlobStorage
{
    /// <summary>
    /// Uploads a stream as a blob and returns the publicly accessible URI.
    /// </summary>
    Task<string> UploadAsync(string blobName, Stream data, string? contentType, CancellationToken cancellationToken = default);
}

