using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class NullBlobStorage
{
    public async Task<string> UploadAsync(string blobName, Stream data, string contentType, CancellationToken cancellationToken = default)
    {
        if (data.CanSeek)
        {
            data.Seek(0, SeekOrigin.Begin);
        }

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
        catch
        {
            return string.Empty;
        }
    }
}

class Program
{
    static async Task<int> Main()
    {
        // Small 1x1 PNG base64 used in seeding
        var base64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVQI12NgYAAAAAMAASsJTYQAAAAASUVORK5CYII=";
        var bytes = Convert.FromBase64String(base64Png);

        var storage = new NullBlobStorage();
        using var ms = new MemoryStream(bytes);
        var result = await storage.UploadAsync("demo/test.png", ms, "image/png").ConfigureAwait(false);

        if (string.IsNullOrEmpty(result))
        {
            Console.WriteLine("Upload returned empty string");
            return 1;
        }

        Console.WriteLine($"Result length: {result.Length}");
        Console.WriteLine($"Prefix: {result.Substring(0, Math.Min(200, result.Length))}");

        return 0;
    }
}

