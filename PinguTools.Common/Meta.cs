using System.ComponentModel;

namespace PinguTools.Common;

public record TimeSignature(int Tick = 0, int Numerator = 4, int Denominator = 4);

public record Meta
{
    public string FilePath { get; set; } = string.Empty;

    public int? Id
    {
        get;
        set
        {
            if (StageId - 1000000 == Id) StageId = value + 1000000;
            if (UnlockEventId - 1000000 == Id) UnlockEventId = value + 1000000;
            field = value;
        }
    }

    public string MgxcId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SortName { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public Entry Genre { get; set; } = new(1000, "自制譜");

    public string Designer { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; } = Difficulty.Master;
    public decimal Level { get; set; }
    public DateTime ReleaseDate { get; set; } = DateTime.Now;
    public decimal MainBpm { get; set; }
    public int MainTil { get; set; }

    public int? UnlockEventId { get; set; }

    public Entry WeTag { get; set; } = Entry.Default;
    public StarDifficulty WeDifficulty { get; set; } = StarDifficulty.NA;

    public string JacketFilePath { get; set; } = string.Empty;
    public string FullJacketFilePath => GetFullPath(JacketFilePath);

    public bool IsCustomStage { get; set; }
    public int? StageId { get; set; }

    public string BgFilePath { get; set; } = string.Empty;
    public string FullBgFilePath => GetFullPath(BgFilePath);

    public Entry NotesFieldLine { get; set; } = new(0, "Orange", "オレンジ");
    public Entry Stage { get; set; } = new(8, "レーベル 共通0008_新イエローリング");

    public string BgmFilePath { get; set; } = string.Empty;
    public string FullBgmFilePath => GetFullPath(BgmFilePath);

    public decimal BgmRealOffset
    {
        get
        {
            if (!BgmEnableBarOffset) return BgManualOffset;
            return BgManualOffset + CalculateOffset(BgmInitialBpm, BgmInitialTimeSignature);
        }
    }

    public decimal BgManualOffset { get; set; }

    public decimal BgmPreviewStart { get; set; }
    public decimal BgmPreviewStop { get; set; }
    public bool BgmEnableBarOffset { get; set; }

    public decimal BgmInitialBpm { get; set; } = 120m;

    public TimeSignature BgmInitialTimeSignature { get; set; } = new();

    public string Comment { get; set; } = string.Empty;

    // Flags
    public bool IsMain { get; set; } = true; // used in option convert
    public bool NormalizeBgm { get; set; } = true;

    private string GetFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)) return path;
        var folder = Path.GetDirectoryName(FilePath);
        return string.IsNullOrWhiteSpace(folder) ? path : Path.GetFullPath(Path.Combine(folder, path));
    }

    private static decimal CalculateOffset(decimal bpm, TimeSignature signature, int bar = 1) // in seconds
    {
        var beatsPerSecond = bpm / 60;
        var beatLength = 1 / beatsPerSecond;
        var measureLength = beatLength * signature.Numerator;
        var fractionOfMeasure = measureLength * (4m / signature.Denominator);
        var offset = bar * fractionOfMeasure;
        return offset;
    }
}

public enum Difficulty
{
    [Description("Basic")]
    Basic = 0,
    [Description("Advanced")]
    Advanced = 1,
    [Description("Expert")]
    Expert = 2,
    [Description("Master")]
    Master = 3,
    [Description("Ultima")]
    Ultima = 4,
    [Description("World's End")]
    WorldsEnd = 5
}

public enum StarDifficulty
{
    [Description("N/A")]
    NA = 0,
    [Description("⭐")]
    S1 = 1,
    [Description("⭐⭐")]
    S2 = 3,
    [Description("⭐⭐⭐")]
    S3 = 5,
    [Description("⭐⭐⭐⭐")]
    S4 = 7,
    [Description("⭐⭐⭐⭐⭐")]
    S5 = 9
}