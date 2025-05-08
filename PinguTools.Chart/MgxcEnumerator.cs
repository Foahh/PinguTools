namespace PinguTools.Chart;

internal static class MgxcEnumerator
{
    public async static Task EnumerateAsync(this TextReader reader, Action<Block, string[]> action)
    {
        var block = Block.ROOT;
        while (await reader.ReadLineAsync() is { } line)
        {
            var args = line.Split('\t');
            if (args.Length == 0) continue;
            if (args[0] == "BEGIN")
            {
                if (args.Length < 2) continue;
                block = args[1] switch
                {
                    "META" => Block.META,
                    "HEADER" => Block.HEADER,
                    "NOTES" => Block.NOTES,
                    _ => block
                };
                continue;
            }
            action(block, args);
        }
    }
}

internal enum Block
{
    ROOT,
    META,
    HEADER,
    NOTES
}