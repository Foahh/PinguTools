namespace PinguTools.Image;

public class SvoChunkLocator : IChunkLocator
{
    public (int Start, int End)[] Locate(byte[] data)
    {
        throw new NotImplementedException();
    }

    public byte[][] Extract(byte[] data, (int Start, int End)[] chunks)
    {
        throw new NotImplementedException();
    }

    public byte[] Replace(byte[] data, (int Start, int End)[] chunks, IReadOnlyList<byte[]?> replacements)
    {
        throw new NotImplementedException();
    }
}