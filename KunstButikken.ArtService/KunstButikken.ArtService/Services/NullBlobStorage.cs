namespace KunstButikken.ArtService.Services;

internal sealed class NullBlobStorage : IBlobStorage
{
    /// <summary>
    ///     No-op upload for development when Azure Blob isn't configured.
    ///     Returns a data URI (base64) of the uploaded content so frontend can render images
    ///     during local development instead of an empty string.
    /// </summary>
    public async Task<string> UploadAsync(string blobName, Stream data, string? contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blobName);
        ArgumentNullException.ThrowIfNull(data);

        // If stream supports seeking, reset to start to be a well-behaved consumer.
        if (data.CanSeek)
        {
            data.Seek(0, SeekOrigin.Begin);
        }

        // Read stream contents into memory and return a data URI so the frontend can display it.
        // This keeps development easy without requiring an Azure Blob account.
        try
        {
            using var ms = new MemoryStream();
            await data.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            var bytes = ms.ToArray();
            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            var base64 = Convert.ToBase64String(bytes);
            var ct = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
            return $"data:{ct};base64,{base64}";
        }
        catch (IOException)
        {
            // I/O failure reading the stream -> return empty data URI behavior preserved.
            return string.Empty;
        }
        catch (ObjectDisposedException)
        {
            // Stream disposed while reading - preserve previous behavior by returning empty.
            return string.Empty;
        }
    }
}
