using Kawazu;
using System.Text.RegularExpressions;
using TinyPinyin;

namespace PinguTools.Text;

public partial class SortNameConverter
{
    private readonly KawazuConverter kawazu = new();

    public async Task<string> GetSortName(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var str = SortNameRegex().Replace(s, "");
        if (string.IsNullOrEmpty(str)) return string.Empty;

        str = await kawazu.Convert(str, To.Katakana);
        str = PinyinHelper.GetPinyin(str);

        return str.ToUpper();
    }

    [GeneratedRegex(@"[^A-Za-z\u3040-\u309F\u4E00-\u9FFF]")]
    private static partial Regex SortNameRegex();
}