using CommunityToolkit.Mvvm.ComponentModel;
using PinguTools.Attributes;
using PinguTools.Audio;
using PinguTools.Localization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PinguTools.Models;

[LocalizableCategoryOrder(nameof(ModelStrings.Category_Song), 0, typeof(ModelStrings))]
[LocalizableCategoryOrder(nameof(ModelStrings.Category_BGM), 1, typeof(ModelStrings))]
[LocalizableCategoryOrder(nameof(ModelStrings.Category_Sync), 2, typeof(ModelStrings))]
[LocalizableCategoryOrder(nameof(ModelStrings.Category_Misc), 3, typeof(ModelStrings))]
public partial class MusicModel : PropertyGridModel
{
    public MusicModel()
    {
        BgmInitialTimeSignature.PropertyChanged += (s, e) => OnPropertyChanged(nameof(RealOffset));
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != nameof(RealOffset)) return;
            var barOffset = BgmEnableBarOffset ? AudioHelper.CalculateOffset(BgmInitialBpm, BgmInitialTimeSignature.Numerator, BgmInitialTimeSignature.Denominator) : 0;
            RealOffset = BgmOffset + barOffset;
        };
    }

    // BGM
    [LocalizableCategory(nameof(ModelStrings.Category_BGM), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_BgmPreviewStart), typeof(ModelStrings))]
    [ObservableProperty]
    [Range(0, int.MaxValue)]
    [PropertyOrder(1)]
    public partial decimal BgmPreviewStart { get; set; }

    [LocalizableCategory(nameof(ModelStrings.Category_BGM), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_BgmPreviewStop), typeof(ModelStrings))]
    [ObservableProperty]
    [Range(0, int.MaxValue)]
    [PropertyOrder(2)]
    public partial decimal BgmPreviewStop { get; set; }

    // Sync
    [LocalizableCategory(nameof(ModelStrings.Category_Sync), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_RealOffset), typeof(ModelStrings))]
    [LocalizableDescription(nameof(ModelStrings.Description_RealOffset), typeof(ModelStrings))]
    [DisplayFormat(DataFormatString = "{0:F7}")]
    [ObservableProperty]
    [ReadOnly(true)]
    [PropertyOrder(-2)]
    public partial decimal RealOffset { get; set; }

    [LocalizableCategory(nameof(ModelStrings.Category_Sync), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_ManualOffset), typeof(ModelStrings))]
    [LocalizableDescription(nameof(ModelStrings.Description_ManualOffset), typeof(ModelStrings))]
    [ObservableProperty]
    [DisplayFormat(DataFormatString = "{0:F7}")]
    [NotifyPropertyChangedFor(nameof(RealOffset))]
    [PropertyOrder(-1)]
    public partial decimal BgmOffset { get; set; }

    [LocalizableCategory(nameof(ModelStrings.Category_Sync), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_InsertBlankMeasure), typeof(ModelStrings))]
    [LocalizableDescription(nameof(ModelStrings.Description_InsertBlankMeasure), typeof(ModelStrings))]
    [DisplayFormat(DataFormatString = "{0:F7}")]
    [NotifyPropertyChangedFor(nameof(RealOffset))]
    [ObservableProperty]
    [PropertyOrder(0)]
    public virtual partial bool BgmEnableBarOffset { get; set; } = false;

    [LocalizableCategory(nameof(ModelStrings.Category_Sync), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_BgmInitialBpm), typeof(ModelStrings))]
    [LocalizableDescription(nameof(ModelStrings.Description_BgmInitialBpm), typeof(ModelStrings))]
    [NotifyPropertyChangedFor(nameof(RealOffset))]
    [ObservableProperty]
    [Range(1, short.MaxValue)]
    [DisplayFormat(DataFormatString = "{0:F7}")]
    [PropertyOrder(1)]
    public virtual partial decimal BgmInitialBpm { get; set; } = 120m;

    [LocalizableCategory(nameof(ModelStrings.Category_Sync), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_BgmInitialTimeSignature), typeof(ModelStrings))]
    [LocalizableDescription(nameof(ModelStrings.Description_BgmInitialTimeSignature), typeof(ModelStrings))]
    [ExpandableObject]
    [NotifyPropertyChangedFor(nameof(RealOffset))]
    [ObservableProperty]
    [PropertyOrder(2)]
    public virtual partial Beat BgmInitialTimeSignature { get; set; } = new();

    public partial class Beat : ObservableValidator
    {
        [LocalizableDisplayName(nameof(ModelStrings.Display_Numerator), typeof(ModelStrings))]
        [ObservableProperty]
        [Range(1, short.MaxValue)]
        public partial int Numerator { get; set; } = 4;

        [LocalizableDisplayName(nameof(ModelStrings.Display_Denominator), typeof(ModelStrings))]
        [ObservableProperty]
        [Range(1, short.MaxValue)]
        public partial int Denominator { get; set; } = 4;

        public override string ToString()
        {
            return $"{Numerator}/{Denominator}";
        }
    }
}