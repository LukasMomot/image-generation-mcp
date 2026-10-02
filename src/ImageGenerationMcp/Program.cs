using ImageGenerationMcp.Abstractions;
using ImageGenerationMcp.Configuration;
using ImageGenerationMcp.Providers.OpenRouter;
using ImageGenerationMcp.Storage;
using ImageGenerationMcp.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
// The built-in HttpClient logs duplicate our own request logs; raise it to Debug via Logging__LogLevel__System.Net.Http.HttpClient if needed.
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

// Configuration from environment variables.
var configuration = builder.Configuration;
builder.Services.Configure<ImageGenerationOptions>(o =>
{
    o.DefaultModel = ReadSetting(configuration, "IMAGE_GEN_DEFAULT_MODEL") ?? ImageGenerationDefaults.Model;
    o.OutputDirectory = ReadSetting(configuration, "IMAGE_GEN_OUTPUT_DIR") ?? ImageGenerationDefaults.OutputDirectory;
});
builder.Services.Configure<OpenRouterOptions>(o =>
{
    o.ApiKey = ReadSetting(configuration, "OPENROUTER_API_KEY");
    o.BaseUrl = ReadSetting(configuration, "OPENROUTER_BASE_URL") ?? OpenRouterOptions.DefaultBaseUrl;
});

// Image generation provider. To switch providers, register a different IImageGenerator implementation here.
builder.Services.AddHttpClient<IImageGenerator, OpenRouterImageGenerator>((services, client) =>
{
    var openRouter = services.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
    client.BaseAddress = new Uri(openRouter.BaseUrl.EndsWith('/') ? openRouter.BaseUrl : openRouter.BaseUrl + "/");
    // High-quality image generation can take a while.
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IImageStorage, LocalImageStorage>();

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ImageGenerationTools>();

var app = builder.Build();

LogStartupConfiguration(app.Services);

await app.RunAsync();

static string? ReadSetting(IConfiguration configuration, string key)
{
    var value = configuration[key];
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

static void LogStartupConfiguration(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ImageGenerationMcp");
    var imageOptions = services.GetRequiredService<IOptions<ImageGenerationOptions>>().Value;
    var openRouterOptions = services.GetRequiredService<IOptions<OpenRouterOptions>>().Value;

    logger.LogInformation("Image Generation MCP server starting (provider: OpenRouter, base URL: {BaseUrl})", openRouterOptions.BaseUrl);
    logger.LogInformation("Default model: {Model}", imageOptions.DefaultModel);
    logger.LogInformation("Output directory: {Directory}", Path.GetFullPath(imageOptions.OutputDirectory));

    if (string.IsNullOrWhiteSpace(openRouterOptions.ApiKey))
    {
        logger.LogWarning("OPENROUTER_API_KEY is not set; generate_image calls will fail until it is configured");
    }
    else
    {
        logger.LogInformation("OpenRouter API key: configured");
    }
}
