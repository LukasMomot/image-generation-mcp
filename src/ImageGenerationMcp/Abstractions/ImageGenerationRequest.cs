namespace ImageGenerationMcp.Abstractions;

/// <summary>
/// A provider-agnostic request to generate one image.
/// </summary>
/// <param name="Prompt">Text description of the image to generate.</param>
/// <param name="Model">Provider-specific model identifier, e.g. <c>openai/gpt-image-2.5-flare</c>.</param>
/// <param name="Quality">Quality level, e.g. <c>low</c>, <c>medium</c>, <c>high</c>.</param>
/// <param name="AspectRatio">Aspect ratio in <c>W:H</c> form, e.g. <c>3:2</c>, or <c>auto</c>.</param>
public sealed record ImageGenerationRequest(string Prompt, string Model, string Quality, string AspectRatio);
