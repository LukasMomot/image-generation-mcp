using System.Text.Json.Serialization;

namespace ImageGenerationMcp.Providers.OpenRouter;

// Wire contracts for POST /api/v1/images.
// See https://openrouter.ai/docs/guides/overview/multimodal/image-generation

internal sealed record OpenRouterImageRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("quality")] string Quality,
    [property: JsonPropertyName("aspect_ratio")] string AspectRatio,
    [property: JsonPropertyName("output_format")] string OutputFormat,
    [property: JsonPropertyName("n")] int N);

internal sealed class OpenRouterImageResponse
{
    [JsonPropertyName("data")]
    public List<OpenRouterImageData>? Data { get; set; }

    [JsonPropertyName("usage")]
    public OpenRouterUsage? Usage { get; set; }
}

internal sealed class OpenRouterImageData
{
    [JsonPropertyName("b64_json")]
    public string? B64Json { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }
}

internal sealed class OpenRouterUsage
{
    [JsonPropertyName("cost")]
    public decimal? Cost { get; set; }
}

internal sealed class OpenRouterErrorResponse
{
    [JsonPropertyName("error")]
    public OpenRouterError? Error { get; set; }
}

internal sealed class OpenRouterError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
