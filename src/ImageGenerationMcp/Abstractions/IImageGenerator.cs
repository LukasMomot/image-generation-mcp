namespace ImageGenerationMcp.Abstractions;

/// <summary>
/// Provider-agnostic image generation service.
/// Implement this interface to plug in a new backend (OpenRouter, OpenAI, Azure, a local model, ...).
/// </summary>
public interface IImageGenerator
{
    /// <summary>
    /// Generates a single image for the given request.
    /// </summary>
    /// <exception cref="ImageGenerationException">The provider failed to generate an image.</exception>
    Task<GeneratedImage> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default);
}
