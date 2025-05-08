using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PinguTools.Image;
using PinguTools.Localization;
using PinguTools.Services;
using System.IO;
using System.Media;

namespace PinguTools.ViewModels;

public partial class JacketTabViewModel : ViewModel
{
    private readonly JacketConverter converter;

    public JacketTabViewModel(JacketConverter converter, ActionService acs) : base(acs)
    {
        this.converter = converter;
        ActionCommand = new AsyncRelayCommand(Convert, acs.CanRun);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputFileName))]
    public partial int? JacketId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputFileName))]
    public partial string JacketPath { get; set; } = string.Empty;

    public string OutputFileName
    {
        get
        {
            if (JacketId is { } id) return $"[CHU_UI_Jacket_{id:0000}.dds]";
            return !string.IsNullOrWhiteSpace(JacketPath) ? $"[CHU_UI_Jacket_{Path.GetFileNameWithoutExtension((string?)JacketPath)}.dds]" : string.Empty;
        }
    }

    private async Task Convert()
    {
        var fileName = JacketId is null ? Path.GetFileNameWithoutExtension((string?)JacketPath) : $"{(int)JacketId:0000}";
        var dlg = new SaveFileDialog
        {
            Filter = FileFilterStrings.DDS,
            FileName = $"CHU_UI_Jacket_{fileName}"
        };
        if (dlg.ShowDialog() != true) return;

        var options = new JacketConverter.Context(JacketPath, dlg.FileName);
        await ActionService.RunAsync((diag, p, ct) => converter.ConvertAsync(options, diag, p, ct));

        SystemSounds.Exclamation.Play();
    }
}