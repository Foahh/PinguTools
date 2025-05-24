namespace PinguTools.Common.Graphic.Locator;

public class DdsChunkLocator : IChunkLocator
{
    private static readonly byte[] Header = "DDS |"u8.ToArray();
    private static readonly byte[] StopSign = "POF0 "u8.ToArray();

    public (int Start, int End)[] Locate(byte[] input)
    {
        var list = new List<(int, int)>();
        var start = Find(input, Header, 0);

        while (start != -1)
        {
            var next = Find(input, Header, start + Header.Length);
            var endByStop = Find(input, StopSign, start + Header.Length);

            if (endByStop != -1 && (next == -1 || endByStop < next))
            {
                list.Add((start, endByStop));
                break;
            }

            if (next == -1)
            {
                list.Add((start, input.Length));
                break;
            }

            list.Add((start, next));
            start = next;
        }
        return list.ToArray();
    }

    public async Task ExtractAsync(byte[] input, string outFolder, string baseName, string extension, (int Start, int End)[] chunks)
    {
        for (var i = 0; i < chunks.Length; i++)
        {
            var (start, end) = chunks[i];
            var length = end - start;

            var path = Path.Combine(outFolder, $"{baseName}_{i + 1:0000}{extension}");
            await using var fs = File.Create(path);
            await fs.WriteAsync(input.AsMemory(start, length));
        }
    }

    public async Task ReplaceAsync(byte[] input, string outPath, (int Start, int End)[] chunks, IReadOnlyList<Stream?> replacements)
    {
        await using var fs = File.Create(outPath);
        var cursor = 0;

        for (var i = 0; i < chunks.Length; i++)
        {
            if (i >= replacements.Count) throw new ArgumentOutOfRangeException(nameof(replacements));
            var (s, e) = chunks[i];
            await fs.WriteAsync(input.AsMemory(cursor, s - cursor));
            var replacement = replacements[i];
            if (replacement is null) await fs.WriteAsync(input.AsMemory(s, e - s));
            else await replacement.CopyToAsync(fs);
            cursor = e;
        }

        await fs.WriteAsync(input.AsMemory(cursor, input.Length - cursor));
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