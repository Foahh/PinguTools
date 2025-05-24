using System.Xml.Serialization;

namespace PinguTools.Common.Xml;

[XmlRoot("StageData")]
public class StageXml : XmlElement<StageXml>
{
    internal StageXml()
    {
    }

    public StageXml(int id, Entry noteFieldLane)
    {
        DataName = $"stage{id:000000}";
        Name = new Entry(id, $"custom_stage_{id}");
        NotesFieldLine = noteFieldLane;
        NotesFieldFile = $"nf_custom_stage_{id}.afb";
        BaseFile = $"st_custom_stage_{id}.afb";
    }

    protected override string FileName => "Stage.xml";

    [XmlElement("dataName")]
    public string DataName { get; set; } = string.Empty;

    [XmlElement("netOpenName")]
    public Entry NetOpenName { get; set; } = XmlConstants.NetOpenName;

    [XmlElement("releaseTagName")]
    public Entry ReleaseTagName { get; set; } = Entry.Default;

    [XmlElement("name")]
    public Entry Name { get; set; } = Entry.Default;

    [XmlElement("notesFieldLine")]
    public Entry NotesFieldLine { get; set; } = Entry.Default;

    [XmlElement("notesFieldFile")]
    public PathElement NotesFieldFile { get; set; } = new();

    [XmlElement("baseFile")]
    public PathElement BaseFile { get; set; } = new();

    [XmlElement("objectFile")]
    public PathElement ObjectFile { get; set; } = new();
}