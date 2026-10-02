using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ImageGenerationMcp.Abstractions;
using ImageGenerationMcp.Configuration;
using ImageGenerationMcp.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ImageGenerationMcp.Tools;

/// <summary>
/// MCP tools for generating images.
/// </summary>
[McpServerToolType]
public sealed partial class ImageGenerationTools(
    IImageGenerator generator,
    IImageStorage storage,
    IOptions<ImageGenerationOptions> options,
    ILogger<ImageGenerationTools> logger)
{
    [McpServerTool(Name = "generate_image", Title = "Generate image", ReadOnly = false, Destructive = false, OpenWorld = true)]
    [Description(
        "Generates an image from a text prompt and saves it as a PNG file on the local disk. " +
        "Returns the absolute path of the saved file; open or read that file to view the image.")]
    public async Task<string> GenerateImage(
        [Description("Detailed description of the image to generate (subject, style, composition, lighting, ...).")]
        string prompt,
        [Description("Optional model id, e.g. 'openai/gpt-image-2.5-flare'. Leave empty to use the server's default model.")]
        string? model = null,
        [Description("Image quality: 'auto', 'low', 'medium', 'high', 'xhigh' or 'max'. Higher is slower and more expensive. Supported values depend on the model.")]
        string quality = ImageGenerationDefaults.Quality,
        [Description("Aspect ratio as 'W:H', e.g. '1:1', '3:2', '2:3', '4:3', '3:4', '16:9', '9:16', '21:9', or 'auto'. Supported values depend on the model.")]
        string aspectRatio = ImageGenerationDefaults.AspectRatio,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new McpException("The 'prompt' parameter must not be empty.");
        }

        var resolvedModel = string.IsNullOrWhiteSpace(model) ? options.Value.DefaultModel : model.Trim();
        var resolvedQuality = string.IsNullOrWhiteSpace(quality) ? ImageGenerationDefaults.Quality : quality.Trim().ToLowerInvariant();
        var resolvedAspectRatio = string.IsNullOrWhiteSpace(aspectRatio) ? ImageGenerationDefaults.AspectRatio : aspectRatio.Trim().ToLowerInvariant();

        if (!AspectRatioPattern().IsMatch(resolvedAspectRatio))
        {
            throw new McpException($"Invalid aspect ratio '{aspectRatio}'. Use the form 'W:H' (for example '3:2') or 'auto'.");
        }

        logger.LogInformation(
            "generate_image called: model={Model}{DefaultMarker}, quality={Quality}, aspectRatio={AspectRatio}",
            resolvedModel, string.IsNullOrWhiteSpace(model) ? " (default)" : "", resolvedQuality, resolvedAspectRatio);

        GeneratedImage image;
        string path;
        try
        {
            image = await generator.GenerateAsync(
                new ImageGenerationRequest(prompt.Trim(), resolvedModel, resolvedQuality, resolvedAspectRatio),
                cancellationToken);
            path = await storage.SaveAsync(image, cancellationToken);
        }
        catch (ImageGenerationException ex)
        {
            logger.LogError("Image generation failed: {Message}", ex.Message);
            throw new McpException($"Image generation failed: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Could not save the generated image");
            throw new McpException($"The image was generated but could not be saved: {ex.Message}", ex);
        }

        var result = new StringBuilder()
            .AppendLine("Image generated successfully.")
            .AppendLine(CultureInfo.InvariantCulture, $"Path: {path}")
            .AppendLine(CultureInfo.InvariantCulture, $"Model: {image.Model}")
            .AppendLine(CultureInfo.InvariantCulture, $"Quality: {resolvedQuality}")
            .AppendLine(CultureInfo.InvariantCulture, $"Aspect ratio: {resolvedAspectRatio}")
            .AppendLine(CultureInfo.InvariantCulture, $"Size: {image.Data.Length / 1024.0:F1} KB");
        if (image.Cost is { } cost)
        {
            result.AppendLine(CultureInfo.InvariantCulture, $"Cost: ${cost:0.####}");
        }

        return result.ToString().TrimEnd();
    }

    [GeneratedRegex(@"^(\d+:\d+|auto)$")]
    private static partial Regex AspectRatioPattern();
}
