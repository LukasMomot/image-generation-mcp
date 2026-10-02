using ImageGenerationMcp.Abstractions;
using ImageGenerationMcp.Configuration;
using ImageGenerationMcp.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace ImageGenerationMcp.Tests;

public sealed class LocalImageStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "image-gen-mcp-tests", Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 2, 8, 22, 33, TimeSpan.Zero));

    public LocalImageStorageTests() => _time.SetLocalTimeZone(TimeZoneInfo.Utc);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private LocalImageStorage CreateStorage(string outputDirectory) =>
        new(
            Options.Create(new ImageGenerationOptions { OutputDirectory = outputDirectory }),
            _time,
            NullLogger<LocalImageStorage>.Instance);

    private static GeneratedImage Image => new(TestImages.Png, "image/png", "test/model", null);

    [Fact]
    public async Task SaveAsync_CreatesMissingDirectory_AndUsesTimestampFileName()
    {
        var outputDirectory = Path.Combine(_root, "output");
        Assert.False(Directory.Exists(outputDirectory));

        var path = await CreateStorage(outputDirectory).SaveAsync(Image);

        Assert.True(Directory.Exists(outputDirectory));
        Assert.Equal(Path.Combine(outputDirectory, "20261002_082233.png"), path);
        Assert.Equal(TestImages.Png, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task SaveAsync_SameSecond_AppendsSuffix()
    {
        var storage = CreateStorage(_root);

        var first = await storage.SaveAsync(Image);
        var second = await storage.SaveAsync(Image);
        var third = await storage.SaveAsync(Image);

        Assert.EndsWith("20261002_082233.png", first);
        Assert.EndsWith("20261002_082233_1.png", second);
        Assert.EndsWith("20261002_082233_2.png", third);
    }

    [Fact]
    public async Task SaveAsync_RelativeDirectory_ReturnsAbsolutePath()
    {
        var relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), _root);

        var path = await CreateStorage(relative).SaveAsync(Image);

        Assert.True(Path.IsPathFullyQualified(path));
        Assert.True(File.Exists(path));
    }
}
