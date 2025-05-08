using System.ComponentModel;

namespace PinguTools.Common;

public class ChartMeta
{
    public int? Id
    {
        get;
        set
        {
            if (field == StageId) StageId = value;
            if (field == WeEventId) WeEventId = value;
            field = value;
        }
    }

    public string MgxcId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SortName { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;

    public string Designer { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; } = Difficulty.Master;
    public decimal Level { get; set; }
    public DateTime ReleaseDate { get; set; } = DateTime.Now;
    public decimal MainBpm { get; set; }
    public int MainTil { get; set; } = 0;

    public int? WeEventId { get; set; }
    public Entry WeTag { get; set; } = Entry.Default;
    public StarDifficulty WeDifficulty { get; set; } = StarDifficulty.NA;

    public string JacketFileName { get; set; } = string.Empty;

    public bool IsCustomBg { get; set; }
    public int? StageId { get; set; }
    public string BgFileName
    {
        get;
        set
        {
            IsCustomBg = !string.IsNullOrEmpty(value);
            field = value;
        }
    } = string.Empty;

    public Entry NotesFieldLine { get; set; } = new(8, "White", "ホワイト");

    public Entry Stage { get; set; } = new(8, "レーベル 共通0008_新イエローリング", null);

    public string BgmFileName { get; set; } = string.Empty;
    public decimal BgmOffset { get; set; }
    public decimal BgmPreviewStart { get; set; }
    public decimal BgmPreviewStop { get; set; }
    public bool BgmEnableBarOffset { get; set; } = true;
    public decimal BgmInitialBpm { get; set; }
    public Beat BgmInitialTimeSignature { get; set; } = new(4, 4);

    public class Beat(int numerator, int denominator)
    {
        public int Numerator { get; set; } = numerator;
        public int Denominator { get; set; } = denominator;
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