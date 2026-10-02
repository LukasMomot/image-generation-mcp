using System.Globalization;
using ImageGenerationMcp.Abstractions;
using ImageGenerationMcp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageGenerationMcp.Storage;

/// <summary>
/// Saves images as <c>yyyyMMdd_HHmmss.png</c> (local time) in the configured output directory.
/// </summary>
public sealed class LocalImageStorage(
    IOptions<ImageGenerationOptions> options,
    TimeProvider timeProvider,
    ILogger<LocalImageStorage> logger) : IImageStorage
{
    private const string TimestampFormat = "yyyyMMdd_HHmmss";
    private const int MaxNameAttempts = 1000;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public async Task<string> SaveAsync(GeneratedImage image, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetFullPath(options.Value.OutputDirectory);
        if (!Directory.Exists(directory))
        {
            logger.LogInformation("Output directory {Directory} does not exist, creating it", directory);
            Directory.CreateDirectory(directory);
        }

        if (!image.Data.AsSpan().StartsWith(PngSignature))
        {
            logger.LogWarning(
                "Provider returned {MediaType} data that is not a PNG; saving it with a .png extension anyway", image.MediaType);
        }

        var baseName = timeProvider.GetLocalNow().ToString(TimestampFormat, CultureInfo.InvariantCulture);

        // Several images in the same second get a numeric suffix: 20261002_082233.png, 20261002_082233_1.png, ...
        for (var attempt = 0; attempt < MaxNameAttempts; attempt++)
        {
            var fileName = attempt == 0 ? $"{baseName}.png" : $"{baseName}_{attempt}.png";
            var path = Path.Combine(directory, fileName);
            if (File.Exists(path))
            {
                continue;
            }

            try
            {
                // CreateNew fails if another request grabbed the same name in the meantime.
                await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await stream.WriteAsync(image.Data, cancellationToken);
            }
            catch (IOException) when (File.Exists(path))
            {
                continue;
            }

            logger.LogInformation("Image saved to {Path} ({Bytes} bytes)", path, image.Data.Length);
            return path;
        }

        throw new IOException($"Could not find a free file name for {baseName}.png in {directory}.");
    }
}
