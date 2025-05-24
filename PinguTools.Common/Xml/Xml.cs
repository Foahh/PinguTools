using PinguTools.Common.Asset;
using System.Xml;
using System.Xml.Serialization;

namespace PinguTools.Common.Xml;

public static class XmlConstants
{
    public const string XmlnsXsi = "http://www.w3.org/2001/XMLSchema-instance";
    public const string XmlnsXsd = "http://www.w3.org/2001/XMLSchema";
    public static readonly Entry NetOpenName = new(2600, "v2_30 00_0");
}

public abstract class XmlElement<T>
{
    protected abstract string FileName { get; }

    [XmlNamespaceDeclarations]
    public XmlSerializerNamespaces Xmlns => new(
    [
        new XmlQualifiedName("xsi", XmlConstants.XmlnsXsi),
        new XmlQualifiedName("xsd", XmlConstants.XmlnsXsd)
    ]);

    public async Task SaveAsync(string directory)
    {
        var serializer = new XmlSerializer(typeof(T));
        Directory.CreateDirectory(directory);
        await using var streamWriter = new StreamWriter(Path.Combine(directory, FileName));
        serializer.Serialize(streamWriter, this);
    }
}

public class PathElement
{
    [XmlElement("path")]
    public string Path { get; set; } = string.Empty;

    public static implicit operator string(PathElement elem)
    {
        return elem.Path;
    }

    public static implicit operator PathElement(string value)
    {
        return new PathElement { Path = value };
    }
}

public class ValueElement
{
    [XmlElement("value")]
    public int Value { get; set; }

    public static implicit operator int(ValueElement elem)
    {
        return elem.Value;
    }

    public static implicit operator ValueElement(int value)
    {
        return new ValueElement { Value = value };
    }
}

public class EntryCollection
{
    [XmlArray("list")]
    [XmlArrayItem("StringID")]
    public List<Entry> List { get; private init; } = [];

    public static implicit operator List<Entry>(EntryCollection elem)
    {
        return elem.List;
    }

    public static implicit operator EntryCollection(List<Entry> value)
    {
        return new EntryCollection { List = value };
    }
}