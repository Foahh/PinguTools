using CommunityToolkit.Mvvm.ComponentModel;
using PinguTools.Attributes;
using PinguTools.Localization;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PinguTools.Models;

public abstract partial class PropertyGridModel : ObservableValidator
{
    [PropertyOrder(-1)]
    [LocalizableCategory(nameof(ModelStrings.Category_Song), typeof(ModelStrings))]
    [LocalizableDisplayName(nameof(ModelStrings.Display_SongId), typeof(ModelStrings))]
    [LocalizableDescription(nameof(ModelStrings.Description_SongId), typeof(ModelStrings))]
    [ObservableProperty]
    public partial int? Id { get; set; }

    // workaround for hiding ObservableValidator's property
    [Browsable(false)]
    [ReadOnly(true)]
    public new bool HasErrors { get; set; }

    partial void OnIdChanged(int? oldValue, int? newValue)
    {
        IdChangedHandler(oldValue, newValue);
    }

    protected virtual void IdChangedHandler(int? oldValue, int? newValue)
    {
        // do nothing
    }
}