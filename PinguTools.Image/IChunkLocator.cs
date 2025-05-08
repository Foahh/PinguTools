namespace PinguTools.Image;

public interface IChunkLocator
{
    (int Start, int End)[] Locate(byte[] data);
    byte[][] Extract(byte[] data, (int Start, int End)[] chunks);
    byte[] Replace(byte[] data, (int Start, int End)[] chunks, IReadOnlyList<byte[]?> replacements);
}