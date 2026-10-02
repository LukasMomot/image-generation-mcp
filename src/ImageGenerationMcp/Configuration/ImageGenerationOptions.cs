namespace ImageGenerationMcp.Configuration;

/// <summary>
/// Provider-independent settings for the image generation tool.
/// </summary>
public sealed class ImageGenerationOptions
{
    /// <summary>Model used when the tool call does not specify one (env: <c>IMAGE_GEN_DEFAULT_MODEL</c>).</summary>
    public string DefaultModel { get; set; } = ImageGenerationDefaults.Model;

    /// <summary>Directory where images are saved (env: <c>IMAGE_GEN_OUTPUT_DIR</c>). Relative paths resolve against the working directory.</summary>
    public string OutputDirectory { get; set; } = ImageGenerationDefaults.OutputDirectory;
}
