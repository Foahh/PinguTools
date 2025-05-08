namespace PinguTools.Image;

public class DdsChunkLocator : IChunkLocator
{
    private static readonly byte[] Header = "DDS |"u8.ToArray();
    private static readonly byte[] StopSign = "POF0 "u8.ToArray();

    public (int Start, int End)[] Locate(byte[] data)
    {
        var list = new List<(int, int)>();
        var start = Find(data, Header, 0);

        while (start != -1)
        {
            var next = Find(data, Header, start + Header.Length);
            var endByStop = Find(data, StopSign, start + Header.Length);

            if (endByStop != -1 && (next == -1 || endByStop < next))
            {
                list.Add((start, endByStop));
                break;
            }

            if (next == -1)
            {
                list.Add((start, data.Length));
                break;
            }

            list.Add((start, next));
            start = next;
        }

        return list.ToArray();
    }

    public byte[][] Extract(byte[] data, (int Start, int End)[] chunks)
    {
        var results = new byte[chunks.Length][];
        for (var i = 0; i < chunks.Length; i++)
        {
            var (s, e) = chunks[i];
            var len = e - s;
            var buf = new byte[len];
            Buffer.BlockCopy(data, s, buf, 0, len);
            results[i] = buf;
        }

        return results;
    }

    public byte[] Replace(byte[] data, (int Start, int End)[] chunks, IReadOnlyList<byte[]?> replacements)
    {
        using var ms = new MemoryStream(data.Length);
        var cursor = 0;

        for (var i = 0; i < chunks.Length; i++)
        {
            if (i >= replacements.Count) throw new ArgumentOutOfRangeException(nameof(replacements));
            var (s, e) = chunks[i];
            ms.Write(data, cursor, s - cursor);
            if (replacements[i] is null) ms.Write(data, s, e - s);
            else ms.Write(replacements[i]!);
            cursor = e;
        }

        ms.Write(data, cursor, data.Length - cursor);
        return ms.ToArray();
    }

    private static int Find(byte[] haystack, byte[] needle, int start)
    {
        for (var i = start; i <= haystack.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length && match; j++)
                match &= haystack[i + j] == needle[j];

            if (match) return i;
        }
        return -1;
    }
}