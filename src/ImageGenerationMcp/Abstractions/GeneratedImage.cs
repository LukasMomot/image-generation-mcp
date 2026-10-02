namespace ImageGenerationMcp.Abstractions;

/// <summary>
/// The result of a successful image generation.
/// </summary>
/// <param name="Data">Raw image bytes.</param>
/// <param name="MediaType">Media type reported by the provider, e.g. <c>image/png</c>.</param>
/// <param name="Model">Model that produced the image.</param>
/// <param name="Cost">Cost in USD, if the provider reports it.</param>
public sealed record GeneratedImage(byte[] Data, string MediaType, string Model, decimal? Cost);
