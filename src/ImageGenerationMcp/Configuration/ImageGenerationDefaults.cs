namespace ImageGenerationMcp.Configuration;

/// <summary>
/// Built-in defaults used when neither the tool call nor the environment specify a value.
/// </summary>
public static class ImageGenerationDefaults
{
    public const string Model = "openai/gpt-image-2.5-flare";
    public const string Quality = "medium";
    public const string AspectRatio = "3:2";
    public const string OutputDirectory = "./output";
}
