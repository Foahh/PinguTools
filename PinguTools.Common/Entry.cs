using System.Text.Json.Serialization;

namespace PinguTools.Common;

public record Entry([property: JsonPropertyName("id")] int Id, [property: JsonPropertyName("str")] string Str, [property: JsonPropertyName("data")] string? Data) : IComparable<Entry>
{
    public static readonly Entry Default = new(-1, "Invalid", null);

    public int CompareTo(Entry? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        var id = Id.CompareTo(other.Id);
        if (id != 0) return id;
        var str = string.Compare(Str, other.Str, StringComparison.Ordinal);
        if (str != 0) return str;
        return string.Compare(Data, other.Data, StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return $"{Id} {Str} {Data}";
    }
}