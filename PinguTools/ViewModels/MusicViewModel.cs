using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using PinguTools.Common;
using PinguTools.Common.Audio;
using PinguTools.Common.Resources;
using PinguTools.Models;
using PinguTools.Resources;
using System.IO;

namespace PinguTools.ViewModels;

public partial class MusicViewModel : ActionViewModel
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ActionCommand))]
    public partial string MusicPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial MusicModel? Model { get; set; }

    partial void OnMusicPathChanged(string value)
    {
        Model = string.IsNullOrWhiteSpace(value) ? null : new MusicModel();
    }

    protected override bool CanRun()
    {
        return !string.IsNullOrWhiteSpace(MusicPath);
    }

    protected async override Task Action()
    {
        if (Model?.Id is null) throw new DiagnosticException(CommonStrings.Error_song_id_is_not_set);
        Model.Meta.BgmFilePath = MusicPath;

        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)MusicPath),
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (dlg.ShowDialog() != true) return;
        var path = dlg.FolderName;

        var converter = new MusicConverter();
        var opts = new MusicConverter.Context(Model.Meta, path);
        await ActionService.RunAsync((diag, p, ct) => converter.ConvertAsync(opts, diag, p, ct));
    }
}