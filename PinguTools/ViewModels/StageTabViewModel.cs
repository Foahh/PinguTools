using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PinguTools.Common;
using PinguTools.Image;
using PinguTools.Localization;
using PinguTools.Services;
using System.IO;
using System.Media;

namespace PinguTools.ViewModels;

public partial class StageTabViewModel : ViewModel
{
    private readonly StageConverter converter;

    public StageTabViewModel(StageConverter converter, ActionService acs, AssetService ass) : base(acs)
    {
        AssetManager = ass;
        NoteFieldsLine = ass.FieldLines.FirstOrDefault(p => p.Str == "Orange") ?? Entry.Default;
        this.converter = converter;
        ActionCommand = new AsyncRelayCommand(Generate, acs.CanRun);
    }

    [ObservableProperty]
    public partial string BackgroundPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EffectPath0 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EffectPath1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EffectPath2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EffectPath3 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Entry NoteFieldsLine { get; set; }

    [ObservableProperty]
    public partial int StageId { get; set; }

    public AssetService AssetManager { get; }

    private async Task Generate()
    {
        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)BackgroundPath),
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false
        };
        if (dlg.ShowDialog() != true) return;

        string[] effectPaths = [EffectPath0, EffectPath1, EffectPath2, EffectPath3];
        var options = new StageConverter.Context(BackgroundPath, effectPaths, StageId, dlg.FolderName, NoteFieldsLine, AssetManager.StageNames);

        await ActionService.RunAsync((diag, p, ct) => converter.ConvertAsync(options, diag, p, ct));
        SystemSounds.Exclamation.Play();
    }

    [RelayCommand]
    private void ClearAll()
    {
        BackgroundPath = string.Empty;
        EffectPath0 = string.Empty;
        EffectPath1 = string.Empty;
        EffectPath2 = string.Empty;
        EffectPath3 = string.Empty;
    }
}