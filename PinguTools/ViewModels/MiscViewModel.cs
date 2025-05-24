using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PinguTools.Common;
using PinguTools.Common.Graphic;
using PinguTools.Common.Graphic.Locator;
using PinguTools.Common.Resources;
using PinguTools.Resources;
using System.Diagnostics;
using System.IO;
using System.Media;

namespace PinguTools.ViewModels;

public partial class MiscViewModel : ViewModel
{
    [RelayCommand]
    private static void OpenTempDirectory()
    {
        var path = ResourceManager.TempPath;
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
            Filter = CommonStrings.Filefilter_afb,
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
            InitialDirectory = baseDir != null ? new DirectoryInfo(baseDir).Name : null,
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (saveDlg.ShowDialog() != true) return;

        var extractor = new ChunkExtractor(new DdsChunkLocator(), ".dds");
        var options = new ChunkExtractor.Options(openDlg.FileName, saveDlg.FolderName);
        await ActionService.RunAsync((diag, p, ct) => extractor.ConvertAsync(options, diag, p, ct));

        SystemSounds.Exclamation.Play();
    }

    [RelayCommand]
    private async Task CollectAsset()
    {
        var openDlg = new OpenFolderDialog
        {
            Title = Strings.Title_select_the_A000_folder,
            Multiselect = false,
            ValidateNames = true
        };
        var result = openDlg.ShowDialog(App.MainWindow);
        if (result is not true || string.IsNullOrWhiteSpace(openDlg.FolderName)) return;
        await ActionService.RunAsync((diag, p, ct) => AssetManager.CollectAssetsAsync(openDlg.FolderName, p, ct));
        SystemSounds.Exclamation.Play();
    }
}