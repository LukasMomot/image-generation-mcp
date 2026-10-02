using System.Net;
using System.Text.Json;
using ImageGenerationMcp.Abstractions;
using ImageGenerationMcp.Providers.OpenRouter;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ImageGenerationMcp.Tests;

public class OpenRouterImageGeneratorTests
{
    private static readonly ImageGenerationRequest Request =
        new("a red panda astronaut", "openai/gpt-image-2.5-flare", "medium", "3:2");

    private static OpenRouterImageGenerator CreateGenerator(FakeHttpMessageHandler handler, string? apiKey = "test-key") =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri(OpenRouterOptions.DefaultBaseUrl) },
            Options.Create(new OpenRouterOptions { ApiKey = apiKey }),
            NullLogger<OpenRouterImageGenerator>.Instance);

    private static string SuccessBody(byte[] image, decimal cost = 0.04m) => JsonSerializer.Serialize(new
    {
        created = 1748372400,
        data = new[] { new { b64_json = Convert.ToBase64String(image), media_type = "image/png" } },
        usage = new { cost },
    });

    [Fact]
    public async Task GenerateAsync_SendsExpectedRequest()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessBody(TestImages.Png));

        await CreateGenerator(handler).GenerateAsync(Request);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://openrouter.ai/api/v1/images", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-key", handler.LastRequest.Headers.Authorization.Parameter);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var root = json.RootElement;
        Assert.Equal("openai/gpt-image-2.5-flare", root.GetProperty("model").GetString());
        Assert.Equal("a red panda astronaut", root.GetProperty("prompt").GetString());
        Assert.Equal("medium", root.GetProperty("quality").GetString());
        Assert.Equal("3:2", root.GetProperty("aspect_ratio").GetString());
        Assert.Equal("png", root.GetProperty("output_format").GetString());
        Assert.Equal(1, root.GetProperty("n").GetInt32());
    }

    [Fact]
    public async Task GenerateAsync_DecodesImageAndCost()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessBody(TestImages.Png, 0.0425m));

        var image = await CreateGenerator(handler).GenerateAsync(Request);

        Assert.Equal(TestImages.Png, image.Data);
        Assert.Equal("image/png", image.MediaType);
        Assert.Equal("openai/gpt-image-2.5-flare", image.Model);
        Assert.Equal(0.0425m, image.Cost);
    }

    [Fact]
    public async Task GenerateAsync_ErrorResponse_ThrowsWithProviderMessage()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.Unauthorized, """{"error":{"code":401,"message":"No auth credentials found"}}""");

        var ex = await Assert.ThrowsAsync<ImageGenerationException>(() => CreateGenerator(handler).GenerateAsync(Request));

        Assert.Contains("401", ex.Message);
        Assert.Contains("No auth credentials found", ex.Message);
    }

    [Fact]
    public async Task GenerateAsync_NoImageInResponse_Throws()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data":[]}""");

        await Assert.ThrowsAsync<ImageGenerationException>(() => CreateGenerator(handler).GenerateAsync(Request));
    }

    [Fact]
    public async Task GenerateAsync_MissingApiKey_ThrowsWithoutCallingApi()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessBody(TestImages.Png));

        var ex = await Assert.ThrowsAsync<ImageGenerationException>(
            () => CreateGenerator(handler, apiKey: null).GenerateAsync(Request));

        Assert.Contains("OPENROUTER_API_KEY", ex.Message);
        Assert.Null(handler.LastRequest);
    }
}
