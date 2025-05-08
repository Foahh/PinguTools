using CommunityToolkit.Mvvm.ComponentModel;
using PinguTools.Attributes;
using PinguTools.Common;
using PinguTools.Localization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PinguTools.Models;

[LocalizableCategoryOrder(nameof(ModelStrings.Category_Song), 0, typeof(ModelStrings))]
[LocalizableCategoryOrder(nameof(ModelStrings.Category_Chart), 1, typeof(ModelStrings))]
[LocalizableCategoryOrder(nameof(ModelStrings.Category_WorldsEnd), 2, typeof(ModelStrings))]
[LocalizableCategoryOrder(nameof(ModelStrings.Category_Display), 3, typeof(ModelStrings))]
[LocalizableCategoryOrder(nameof(ModelStrings.Category_BGM), 4, typeof(ModelStrings))]
[LocalizableCategoryOrder(nameof(ModelStrings.Category_Sync), 5, typeof(ModelStrings))]
[LocalizableCategoryOrder(nameof(ModelStrings.Category_Misc), 6, typeof(ModelStrings))]
public partial class WorkflowModel : MusicModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Song
    [LocalizableCategory(nameof(ModelStrings.Category_Song), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_Title), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(0)]
    public partial string Title { get; set; } = string.Empty;

    [LocalizableCategory(nameof(ModelStrings.Category_Song), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_SortName), typeof(ModelStrings))]
    [LocalizableDescription(nameof(ModelStrings.Description_SortName), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(1)]
    public partial string SortName { get; set; } = string.Empty;

    [LocalizableCategory(nameof(ModelStrings.Category_Song), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_Artist), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(2)]
    public partial string Artist { get; set; } = string.Empty;

    // Chart
    [LocalizableCategory(nameof(ModelStrings.Category_Chart), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_Designer), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(0)]
    public partial string Designer { get; set; } = string.Empty;

    [LocalizableCategory(nameof(ModelStrings.Category_Chart), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_Difficulty), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(1)]
    public partial Difficulty Difficulty { get; set; } = Difficulty.Master;

    [LocalizableCategory(nameof(ModelStrings.Category_Chart), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_Level), typeof(ModelStrings))]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    [ObservableProperty]
    [PropertyOrder(2)]
    public partial decimal Level { get; set; }

    [LocalizableCategory(nameof(ModelStrings.Category_Chart), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_ReleaseDate), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(5)]
    public partial DateTime ReleaseDate { get; set; } = DateTime.Now;

    [LocalizableCategory(nameof(ModelStrings.Category_Chart), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_DisplayBPM), typeof(ModelStrings))]
    [DisplayFormat(DataFormatString = "{0:F6}")]
    [ObservableProperty]
    [PropertyOrder(6)]
    public partial decimal MainBpm { get; set; }

    // World's End
    [PropertyOrder(-1)]
    [LocalizableCategory(nameof(ModelStrings.Category_WorldsEnd), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_WorldsEndEventID), typeof(ModelStrings))]
    [LocalizableDescription(nameof(ModelStrings.Description_WorldsEndEventID), typeof(ModelStrings))]
    [ObservableProperty]
    public partial int? WeEventId { get; set; }

    [LocalizableCategory(nameof(ModelStrings.Category_WorldsEnd), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_WorldsEndTag), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(0)]
    public partial Entry WeTag { get; set; } = Entry.Default;

    [LocalizableCategory(nameof(ModelStrings.Category_WorldsEnd), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_WorldsEndDifficulty), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(1)]
    public partial StarDifficulty WeDifficulty { get; set; } = StarDifficulty.NA;

    // Display
    [LocalizableCategory(nameof(ModelStrings.Category_Display), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_JacketFile), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(0)]
    public partial string JacketFileName { get; set; } = string.Empty;

    [LocalizableCategory(nameof(ModelStrings.Category_Display), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_IsCustomBackground), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(1)]
    public partial bool IsCustomBg { get; set; } = false;

    [LocalizableCategory(nameof(ModelStrings.Category_Display), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_StageId), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(2)]
    public partial int? StageId { get; set; } = null;

    [LocalizableCategory(nameof(ModelStrings.Category_Display), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_BackgroundFile), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(3)]
    public partial string BgFileName { get; set; } = string.Empty;

    [LocalizableCategory(nameof(ModelStrings.Category_Display), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_NotesFieldLine), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(4)]
    public partial Entry NotesFieldLine { get; set; } = new(8, "White", "ホワイト");

    [LocalizableCategory(nameof(ModelStrings.Category_Display), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_BackgroundStage), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(5)]
    public partial Entry Stage { get; set; } = new(8, "レーベル 共通0008_新イエローリング", null);

    // BGM
    [LocalizableCategory(nameof(ModelStrings.Category_BGM), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_BGMFile), typeof(ModelStrings))]
    [LocalizableDescription(nameof(ModelStrings.Description_BGMFile), typeof(ModelStrings))]
    [ObservableProperty]
    [PropertyOrder(0)]
    public partial string BgmFileName { get; set; } = string.Empty;

    [ObservableProperty]
    [Browsable(false)]
    [ReadOnly(true)]
    public partial string RootPath { get; set; } = string.Empty;

    [ObservableProperty]
    [Browsable(false)]
    [ReadOnly(true)]
    public partial string? MgxcId { get; set; }

    // Read-only properties
    [ReadOnly(true)]
    public override bool BgmEnableBarOffset
    {
        get => base.BgmEnableBarOffset;
        set => base.BgmEnableBarOffset = value;
    }

    [ReadOnly(true)]
    public override decimal BgmInitialBpm
    {
        get => base.BgmInitialBpm;
        set => base.BgmInitialBpm = value;
    }

    [ReadOnly(true)]
    public override Beat BgmInitialTimeSignature
    {
        get => base.BgmInitialTimeSignature;
        set => base.BgmInitialTimeSignature = value;
    }

    public static WorkflowModel Create(ChartMeta meta)
    {
        ArgumentNullException.ThrowIfNull(meta);
        var json = JsonSerializer.Serialize(meta);
        var model = JsonSerializer.Deserialize<WorkflowModel>(json, JsonOptions);
        if (model is null) throw new JsonException("Failed to deserialize WorkflowModel.");
        return model;
    }

    public ChartMeta ToMeta()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        var meta = JsonSerializer.Deserialize<ChartMeta>(json, JsonOptions);
        if (meta is null) throw new JsonException("Failed to deserialize ChartMeta.");
        return meta;
    }

    protected override void IdChangedHandler(int? oldValue, int? newValue)
    {
        if (WeEventId == oldValue) WeEventId = newValue;
        if (StageId == oldValue) StageId = newValue;
    }

    partial void OnRootPathChanged(string value)
    {
        if (string.IsNullOrEmpty(RootPath)) return;
        if (!string.IsNullOrEmpty(BgmFileName) && !Path.IsPathRooted(BgmFileName)) BgmFileName = Path.GetFullPath(Path.Combine(RootPath, BgmFileName));
        if (!string.IsNullOrEmpty(JacketFileName) && !Path.IsPathRooted(JacketFileName)) JacketFileName = Path.GetFullPath(Path.Combine(RootPath, JacketFileName));
        if (!string.IsNullOrEmpty(BgFileName) && !Path.IsPathRooted(BgFileName)) BgFileName = Path.GetFullPath(Path.Combine(RootPath, BgFileName));
    }

    partial void OnBgmFileNameChanged(string value)
    {
        if (string.IsNullOrEmpty(RootPath)) return;
        if (!string.IsNullOrEmpty(value) && !Path.IsPathRooted(value)) BgmFileName = Path.GetFullPath(Path.Combine(RootPath, value));
    }

    partial void OnJacketFileNameChanged(string value)
    {
        if (string.IsNullOrEmpty(RootPath)) return;
        if (!string.IsNullOrEmpty(value) && !Path.IsPathRooted(value)) JacketFileName = Path.GetFullPath(Path.Combine(RootPath, value));
    }

    partial void OnBgFileNameChanged(string value)
    {
        if (string.IsNullOrEmpty(RootPath)) return;
        if (!string.IsNullOrEmpty(value) && !Path.IsPathRooted(value)) BgFileName = Path.GetFullPath(Path.Combine(RootPath, value));
    }
}