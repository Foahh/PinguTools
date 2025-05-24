namespace PinguTools.Common.Graphic.Locator;

public interface IChunkLocator
{
    (int Start, int End)[] Locate(byte[] input);
    Task ExtractAsync(byte[] input, string outFolder, string baseName, string extension, (int Start, int End)[] chunks);
    Task ReplaceAsync(byte[] input, string outPath, (int Start, int End)[] chunks, IReadOnlyList<Stream?> replacements);
}