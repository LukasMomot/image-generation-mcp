using ImageGenerationMcp.Abstractions;
using ImageGenerationMcp.Configuration;
using ImageGenerationMcp.Storage;
using ImageGenerationMcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol;

namespace ImageGenerationMcp.Tests;

public class ImageGenerationToolsTests
{
    private sealed class RecordingGenerator : IImageGenerator
    {
        public ImageGenerationRequest? LastRequest { get; private set; }
        public Exception? ThrowOnGenerate { get; init; }

        public Task<GeneratedImage> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            if (ThrowOnGenerate is not null)
            {
                throw ThrowOnGenerate;
            }

            return Task.FromResult(new GeneratedImage(TestImages.Png, "image/png", request.Model, 0.04m));
        }
    }

    private sealed class FixedPathStorage : IImageStorage
    {
        public Task<string> SaveAsync(GeneratedImage image, CancellationToken cancellationToken = default) =>
            Task.FromResult("/tmp/output/20261002_082233.png");
    }

    private static ImageGenerationTools CreateTools(IImageGenerator generator, string defaultModel = "env/default-model") =>
        new(
            generator,
            new FixedPathStorage(),
            Options.Create(new ImageGenerationOptions { DefaultModel = defaultModel }),
            NullLogger<ImageGenerationTools>.Instance);

    [Fact]
    public async Task GenerateImage_NoModel_UsesDefaultsFromConfiguration()
    {
        var generator = new RecordingGenerator();

        var result = await CreateTools(generator).GenerateImage("a cat");

        Assert.Equal(new ImageGenerationRequest("a cat", "env/default-model", "medium", "3:2"), generator.LastRequest);
        Assert.Contains("Path: /tmp/output/20261002_082233.png", result);
        Assert.Contains("Cost: $0.04", result);
    }

    [Fact]
    public async Task GenerateImage_ExplicitParameters_ArePassedThrough()
    {
        var generator = new RecordingGenerator();

        await CreateTools(generator).GenerateImage("a cat", model: "other/model", quality: "HIGH", aspectRatio: "16:9");

        Assert.Equal(new ImageGenerationRequest("a cat", "other/model", "high", "16:9"), generator.LastRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateImage_EmptyPrompt_Throws(string prompt)
    {
        await Assert.ThrowsAsync<McpException>(() => CreateTools(new RecordingGenerator()).GenerateImage(prompt));
    }

    [Theory]
    [InlineData("wide")]
    [InlineData("3x2")]
    [InlineData("3:")]
    public async Task GenerateImage_InvalidAspectRatio_Throws(string aspectRatio)
    {
        var generator = new RecordingGenerator();

        await Assert.ThrowsAsync<McpException>(() => CreateTools(generator).GenerateImage("a cat", aspectRatio: aspectRatio));
        Assert.Null(generator.LastRequest);
    }

    [Fact]
    public async Task GenerateImage_ProviderFailure_SurfacesAsMcpException()
    {
        var generator = new RecordingGenerator { ThrowOnGenerate = new ImageGenerationException("OpenRouter returned HTTP 402: Insufficient credits") };

        var ex = await Assert.ThrowsAsync<McpException>(() => CreateTools(generator).GenerateImage("a cat"));

        Assert.Contains("Insufficient credits", ex.Message);
    }
}
