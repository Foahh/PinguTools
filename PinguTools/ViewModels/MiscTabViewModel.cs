using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PinguTools.Image;
using PinguTools.Localization;
using PinguTools.Services;
using System.Diagnostics;
using System.IO;
using System.Media;

namespace PinguTools.ViewModels;

public partial class MiscTabViewModel(AfbExtractor afbExtractor, ActionService acs, AssetService ass) : ViewModel(acs)
{
    [RelayCommand]
    private static void OpenTempDirectory()
    {
        var path = App.TempDir;
        var actualPath = path;
        var attr = File.GetAttributes(path);
        if (!attr.HasFlag(FileAttributes.Directory))
        {
            var dir = Path.GetDirectoryName(path);
            if (dir is not null) actualPath = dir;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = actualPath,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private async Task ExtractAfbFile()
    {
        var openDlg = new OpenFileDialog
        {
            Title = Strings.Title_select_the_input_file,
            Filter = FileFilterStrings.Afb,
            CheckFileExists = true,
            AddExtension = true,
            ValidateNames = true
        };
        var result = openDlg.ShowDialog(App.MainWindow);
        if (result is not true || string.IsNullOrWhiteSpace(openDlg.FileName)) return;

        var baseDir = Path.GetDirectoryName(openDlg.FileName);
        var saveDlg = new OpenFolderDialog
        {
            FolderName = Path.GetFileNameWithoutExtension(openDlg.FileName),
            ValidateNames = true,
            InitialDirectory = baseDir != null ? new DirectoryInfo(baseDir).Name : null,
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false
        };
        if (saveDlg.ShowDialog() != true) return;

        var options = new AfbExtractor.Options(openDlg.FileName, saveDlg.FolderName);
        await ActionService.RunAsync((diag, p, ct) => afbExtractor.ConvertAsync(options, diag, p, ct));

        SystemSounds.Exclamation.Play();
    }


    [RelayCommand]
    private async Task CollectAsset()
    {
        var openDlg = new OpenFolderDialog
        {
            Title = Strings.Title_select_the_A000_folder
        };
        var result = openDlg.ShowDialog(App.MainWindow);
        if (result is not true || string.IsNullOrWhiteSpace(openDlg.FolderName)) return;
        await ActionService.RunAsync((diag, p, ct) => ass.CollectAssetsAsync(openDlg.FolderName, p, ct));
        SystemSounds.Exclamation.Play();
    }
}