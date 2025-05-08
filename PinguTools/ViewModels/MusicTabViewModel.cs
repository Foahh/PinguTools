using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PinguTools.Audio;
using PinguTools.Common.Localization;
using PinguTools.Localization;
using PinguTools.Models;
using PinguTools.Services;
using System.IO;

namespace PinguTools.ViewModels;

public partial class MusicTabViewModel : ViewModel
{
    private readonly MusicConverter converter;

    public MusicTabViewModel(MusicConverter converter, LoudNormalizer normalizer, ActionService acs) : base(acs)
    {
        this.converter = converter;
        Normalizer = normalizer;
        ActionCommand = new AsyncRelayCommand(Convert, acs.CanRun);
    }

    [ObservableProperty]
    public partial string MusicPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial MusicModel? Model { get; set; }

    [ObservableProperty]
    public partial LoudNormalizer Normalizer { get; set; }

    partial void OnMusicPathChanged(string value)
    {
        Model = string.IsNullOrEmpty(value) ? null : new MusicModel();
    }

    private async Task Convert()
    {
        if (Model?.Id is null) throw new OperationCanceledException(CommonStrings.Error_song_id_is_not_set);

        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)MusicPath),
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false
        };
        if (dlg.ShowDialog() != true) return;
        var path = dlg.FolderName;

        var opts = new MusicConverter.Context(MusicPath, path, (int)Model.Id, (double)Model.RealOffset, (double)Model.BgmPreviewStart, (double)Model.BgmPreviewStop);
        await ActionService.RunAsync((diag, p, ct) => converter.ConvertAsync(opts, diag, p, ct));
    }
}