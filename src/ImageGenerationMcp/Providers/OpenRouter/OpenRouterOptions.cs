namespace ImageGenerationMcp.Providers.OpenRouter;

/// <summary>
/// Settings for the OpenRouter image generation provider.
/// </summary>
public sealed class OpenRouterOptions
{
    public const string DefaultBaseUrl = "https://openrouter.ai/api/v1/";

    /// <summary>OpenRouter API key (env: <c>OPENROUTER_API_KEY</c>).</summary>
    public string? ApiKey { get; set; }

    /// <summary>API base URL (env: <c>OPENROUTER_BASE_URL</c>). Must end with a slash.</summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;
}
