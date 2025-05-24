using PinguTools.Common.Resources;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PinguTools.Common.Asset;

public class AssetManager : INotifyPropertyChanged
{
    private const string PATH = "assets.json";

    public AssetManager()
    {
        MergeAssets.MergeWith(HardAssets, new AssetDictionary(PATH));
        NotifyAssetChanged();
    }

    public AssetDictionary MergeAssets { get; private set; } = new();
    private AssetDictionary HardAssets { get; } = new(CommonResources.assets_json);
    private AssetDictionary UserAssets { get; } = new();

    public IReadOnlySet<Entry> this[AssetType type] => MergeAssets[type];
    public IReadOnlySet<Entry> GenreNames => MergeAssets[AssetType.GenreNames];
    public IReadOnlySet<Entry> FieldLines => MergeAssets[AssetType.FieldLines];
    public IReadOnlySet<Entry> StageNames => MergeAssets[AssetType.StageNames];
    public IReadOnlySet<Entry> WeTagNames => MergeAssets[AssetType.WeTagNames];

    public async Task CollectAssetsAsync(string workDir, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!Directory.Exists(workDir)) return;

        progress?.Report(CommonStrings.Status_collecting);
        var collected = await AssetDictionary.CollectAsync(workDir, ct);
        progress?.Report(CommonStrings.Status_saving);
        await collected.SaveAsync(PATH, ct);

        MergeAssets = new AssetDictionary();
        MergeAssets.MergeWith(HardAssets, UserAssets, collected);
        NotifyAssetChanged();
    }

    public void DefineEntry(AssetType type, Entry entry)
    {
        UserAssets[type].Add(entry);
        MergeAssets[type].Add(entry);
        NotifyAssetChanged(type);
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void NotifyAssetChanged(AssetType? type = null)
    {
        OnPropertyChanged(nameof(MergeAssets));

        if (type is not null)
        {
            OnPropertyChanged(type.ToString());
            return;
        }

        OnPropertyChanged(nameof(GenreNames));
        OnPropertyChanged(nameof(FieldLines));
        OnPropertyChanged(nameof(StageNames));
        OnPropertyChanged(nameof(WeTagNames));
    }

    #endregion
}