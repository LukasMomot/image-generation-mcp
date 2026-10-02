using ImageGenerationMcp.Abstractions;

namespace ImageGenerationMcp.Storage;

/// <summary>
/// Persists generated images.
/// </summary>
public interface IImageStorage
{
    /// <summary>
    /// Saves the image and returns the absolute path of the written file.
    /// </summary>
    Task<string> SaveAsync(GeneratedImage image, CancellationToken cancellationToken = default);
}
