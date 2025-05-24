using CommunityToolkit.Mvvm.ComponentModel;
using PinguTools.Common;
using PinguTools.Common.Resources;
using PinguTools.Controls;
using System.Media;

namespace PinguTools.Services;

public partial class ActionService : ObservableObject
{
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial string Status { get; set; } = CommonStrings.Status_idle;
    [ObservableProperty] public partial DateTime StatusTime { get; set; } = DateTime.Now;

    public bool CanRun()
    {
        return !IsBusy;
    }

    public async Task RunAsync(Func<IDiagnostic, IProgress<string>, CancellationToken, Task> action, CancellationToken? externalToken = null)
    {
        if (!CanRun()) return;
        var diagnostics = new DiagnosticReporter();
        IsBusy = true;


        var progress = new Progress<string>(s =>
        {
            Status = s;
            StatusTime = DateTime.Now;
        });
        IProgress<string> ip = progress;

        Exception? lastException = null;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken ?? CancellationToken.None);

            ip.Report(CommonStrings.Status_starting);
            await Task.Run(() => action(diagnostics, progress, cts.Token), cts.Token);
            ip.Report(CommonStrings.Status_done);

            SystemSounds.Exclamation.Play();
        }
        catch (Exception ex)
        {
            diagnostics.Report(ex);
            lastException = ex;
        }
        finally
        {
            IsBusy = false;
        }

        var model = new DiagnosticsWindowViewModel
        {
            Diagnostics = [..diagnostics.Diagnostics],
            StackTrace = lastException?.StackTrace
        };

        if (!diagnostics.HasProblems) return;
        if (diagnostics.HasErrors) ip.Report(CommonStrings.Status_error);

        var window = new DiagnosticsWindow
        {
            DataContext = model,
            Owner = App.MainWindow
        };
        window.ShowDialog();
    }
}