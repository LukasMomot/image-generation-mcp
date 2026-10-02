namespace ImageGenerationMcp.Tests;

internal static class TestImages
{
    // PNG signature followed by a few dummy bytes; enough for the storage PNG check.
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
}
