using PinguTools.Common;
using PinguTools.Localization;
using PinguTools.Misc;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace PinguTools.Services;

public sealed class AssetService(string solidAssetsPath, string dynamicAssetsPath)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private readonly Dictionary<string, ObservableSortedSet<Entry>> dynamicAssets = LoadAssets(dynamicAssetsPath) ?? new Dictionary<string, ObservableSortedSet<Entry>>();
    private readonly Dictionary<string, ObservableSortedSet<Entry>> mergedAssets = new();
    private readonly Dictionary<string, ObservableSortedSet<Entry>> solidAssets = LoadAssets(solidAssetsPath) ?? new Dictionary<string, ObservableSortedSet<Entry>>();

    private string AssetPath { get; } = dynamicAssetsPath;

    public IReadOnlyCollection<Entry> WeTagNames => GetSection("worldsEndTagName");
    public IReadOnlyCollection<Entry> StageNames => GetSection("stageName");
    public IReadOnlyCollection<Entry> FieldLines => GetSection("notesFieldLine");

    private void SetAssets(Dictionary<string, SortedSet<Entry>> value)
    {
        Replace(value, dynamicAssets);
    }

    public async Task CollectAssetsAsync(string workDir, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var specs = new (string FileName, string EntryName)[]
        {
            ("Music.xml", "worldsEndTagName"),
            ("Music.xml", "stageName"),
            ("Stage.xml", "notesFieldLine")
        };

        progress?.Report(Strings.Status_collecting);
        var collected = CollectMany(workDir, specs);

        ct.ThrowIfCancellationRequested();

        progress?.Report(Strings.Status_saving);
        var json = JsonSerializer.Serialize(collected, Options);
        await File.WriteAllTextAsync(AssetPath, json, ct);
        SetAssets(collected);
    }

    private static Dictionary<string, ObservableSortedSet<Entry>>? LoadAssets(string inPath)
    {
        var jsonPath = Path.Combine(inPath);
        if (!File.Exists(jsonPath)) return null;

        var json = File.ReadAllText(jsonPath);
        var source = JsonSerializer.Deserialize<Dictionary<string, SortedSet<Entry>>>(json, Options);
        if (source is null) return null;

        var target = new Dictionary<string, ObservableSortedSet<Entry>>();
        Replace(source, target);
        return target;
    }

    private static void Replace(Dictionary<string, SortedSet<Entry>> source, Dictionary<string, ObservableSortedSet<Entry>> target)
    {
        foreach (var (key, value) in source)
        {
            if (!target.TryGetValue(key, out var set))
            {
                set = [];
                target[key] = set;
            }
            set.UnionWith(value);
        }
    }

    private static void Clear(Dictionary<string, ObservableSortedSet<Entry>> source)
    {
        foreach (var set in source.Values) set.Clear();
    }

    private ObservableSortedSet<Entry> GetSection(string sectionName)
    {
        if (mergedAssets.TryGetValue(sectionName, out var cached) && cached.Count > 0) return cached;
        var merged = cached ?? [];
        merged.Add(Entry.Default);
        if (dynamicAssets.TryGetValue(sectionName, out var entries)) merged.UnionWith(entries);
        if (solidAssets.TryGetValue(sectionName, out var solidEntries)) merged.UnionWith(solidEntries);
        mergedAssets[sectionName] = merged;
        return merged;
    }

    #region Reader

    private static IEnumerable<string> StageFiles(string root, string fileName)
    {
        return Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories);
    }

    private static Entry? ReadEntry(string path, string entryName)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Load(path, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
        catch (XmlException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }

        var node = doc.Root?.Element(entryName);
        if (node is null) return null;

        var idTxt = (node.Element("id")?.Value ?? string.Empty).Trim();
        var strTxt = (node.Element("str")?.Value ?? string.Empty).Trim();
        var dataTxt = (node.Element("data")?.Value ?? string.Empty).Trim();

        if (!int.TryParse(idTxt, out var idVal)) return null;

        var strVal = string.IsNullOrEmpty(strTxt) ? Entry.Default.Str : strTxt;
        var dataVal = string.IsNullOrEmpty(dataTxt) ? Entry.Default.Data : dataTxt;
        return new Entry(idVal, strVal, dataVal);
    }

    private static SortedSet<Entry> CollectOne(string root, string fileName, string entryName)
    {
        var result = new SortedSet<Entry>();
        foreach (var xmlFile in StageFiles(root, fileName))
        {
            var entry = ReadEntry(xmlFile, entryName);
            if (entry != null) result.Add(entry);
        }
        return result;
    }

    private static Dictionary<string, SortedSet<Entry>> CollectMany(string root, IEnumerable<(string FileName, string EntryName)> specs)
    {
        var aggregated = new Dictionary<string, SortedSet<Entry>>();
        foreach (var (fileName, entryName) in specs)
        {
            if (!aggregated.TryGetValue(entryName, out var set))
            {
                set = [];
                aggregated[entryName] = set;
            }
            set.UnionWith(CollectOne(root, fileName, entryName));
        }
        return aggregated;
    }

    #endregion
}