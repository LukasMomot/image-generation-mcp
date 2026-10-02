namespace ImageGenerationMcp.Abstractions;

/// <summary>
/// Thrown by an <see cref="IImageGenerator"/> when an image could not be generated.
/// The message is meant to be shown to the caller (the LLM), so keep it readable and free of secrets.
/// </summary>
public sealed class ImageGenerationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
