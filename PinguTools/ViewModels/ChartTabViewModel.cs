using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PinguTools.Chart;
using PinguTools.Common.Localization;
using PinguTools.Localization;
using PinguTools.Models;
using PinguTools.Services;
using System.IO;
using System.Windows.Threading;

namespace PinguTools.ViewModels;

public partial class ChartTabViewModel : ViewModel
{
    private readonly ChartConverter converter;

    public ChartTabViewModel(ChartConverter converter, ActionService acs) : base(acs)
    {
        this.converter = converter;
        ActionCommand = new AsyncRelayCommand(Convert, ActionService.CanRun);
    }

    [ObservableProperty]
    public partial string ChartPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial WorkflowModel? Model { get; set; }

    async partial void OnChartPathChanged(string value)
    {
        if (string.IsNullOrEmpty(ChartPath)) return;

        await ActionService.RunAsync(async (diag, p, ct) =>
        {
            p.Report(CommonStrings.Status_reading);
            try
            {
                var meta = await converter.ParseMeta(ChartPath, diag);
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    Model = WorkflowModel.Create(meta);
                    Model.RootPath = Path.GetDirectoryName(ChartPath) ?? string.Empty;
                });
            }
            catch
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    ChartPath = string.Empty;
                    Model = null;
                });
                throw;
            }
        });
    }

    private async Task Convert()
    {
        var left = Model?.Id is null ? Path.GetFileNameWithoutExtension((string?)ChartPath) : $"{(int)Model.Id:0000}";
        var right = Model?.Difficulty is null ? string.Empty : $"_{(int)Model.Difficulty:00}";

        var dlg = new SaveFileDialog
        {
            Filter = FileFilterStrings.C2S,
            FileName = left + right
        };
        if (dlg.ShowDialog() != true) return;

        var meta = Model?.ToMeta();
        var options = new ChartConverter.Context(ChartPath, dlg.FileName, meta);
        await ActionService.RunAsync((diag, p, ct) => converter.ConvertAsync(options, diag, p, ct));
    }
}