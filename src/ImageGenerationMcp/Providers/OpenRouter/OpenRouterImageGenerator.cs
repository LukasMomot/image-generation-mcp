using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ImageGenerationMcp.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageGenerationMcp.Providers.OpenRouter;

/// <summary>
/// <see cref="IImageGenerator"/> backed by the OpenRouter Images API (<c>POST /api/v1/images</c>).
/// </summary>
public sealed class OpenRouterImageGenerator(
    HttpClient httpClient,
    IOptions<OpenRouterOptions> options,
    ILogger<OpenRouterImageGenerator> logger) : IImageGenerator
{
    private const string OutputFormat = "png";

    public async Task<GeneratedImage> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ImageGenerationException(
                "OpenRouter API key is not configured. Set the OPENROUTER_API_KEY environment variable in your MCP client configuration.");
        }

        var body = new OpenRouterImageRequest(
            request.Model, request.Prompt, request.Quality, request.AspectRatio, OutputFormat, N: 1);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "images")
        {
            Content = JsonContent.Create(body),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        logger.LogInformation(
            "Requesting image from OpenRouter: model={Model}, quality={Quality}, aspectRatio={AspectRatio}, promptLength={PromptLength}",
            request.Model, request.Quality, request.AspectRatio, request.Prompt.Length);
        logger.LogDebug("Prompt: {Prompt}", request.Prompt);

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "OpenRouter request timed out after {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
            throw new ImageGenerationException(
                $"OpenRouter request timed out after {stopwatch.Elapsed.TotalSeconds:F0} seconds. Try a lower quality setting.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not reach OpenRouter");
            throw new ImageGenerationException($"Could not reach OpenRouter: {ex.Message}", ex);
        }

        using (response)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation(
                "OpenRouter responded {StatusCode} in {ElapsedMs} ms", (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("OpenRouter error response body: {Body}", content);
                var message = TryReadErrorMessage(content) ?? response.ReasonPhrase ?? "Unknown error";
                throw new ImageGenerationException(
                    $"OpenRouter returned HTTP {(int)response.StatusCode}: {message}");
            }

            var result = Deserialize(content);
            var image = result.Data?.FirstOrDefault(d => !string.IsNullOrEmpty(d.B64Json) || !string.IsNullOrEmpty(d.Url))
                ?? throw new ImageGenerationException("OpenRouter returned no image data.");

            var bytes = await ReadImageBytesAsync(image, cancellationToken);
            var mediaType = image.MediaType ?? "image/png";
            var cost = result.Usage?.Cost;

            logger.LogInformation(
                "Image received: {Bytes} bytes, mediaType={MediaType}, cost={Cost}",
                bytes.Length, mediaType, cost?.ToString() ?? "n/a");

            return new GeneratedImage(bytes, mediaType, request.Model, cost);
        }
    }

    private OpenRouterImageResponse Deserialize(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<OpenRouterImageResponse>(content)
                ?? throw new ImageGenerationException("OpenRouter returned an empty response.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Could not parse OpenRouter response");
            throw new ImageGenerationException("OpenRouter returned a response that could not be parsed.", ex);
        }
    }

    private async Task<byte[]> ReadImageBytesAsync(OpenRouterImageData image, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(image.B64Json))
        {
            var base64 = image.B64Json;
            // Tolerate data URLs ("data:image/png;base64,....").
            var comma = base64.IndexOf(',');
            if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
            {
                base64 = base64[(comma + 1)..];
            }

            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new ImageGenerationException("OpenRouter returned image data that is not valid base64.", ex);
            }
        }

        // Some providers return a URL instead of inline data.
        logger.LogInformation("Downloading image from {Url}", image.Url);
        try
        {
            return await httpClient.GetByteArrayAsync(image.Url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ImageGenerationException($"Could not download the generated image: {ex.Message}", ex);
        }
    }

    private static string? TryReadErrorMessage(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<OpenRouterErrorResponse>(content)?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
