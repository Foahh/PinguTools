using CommunityToolkit.Mvvm.ComponentModel;
using PinguTools.Common;
using PinguTools.Controls;
using PinguTools.Localization;
using System.Media;

namespace PinguTools.Services;

public partial class ActionService : ObservableObject
{
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial string Status { get; set; } = Strings.Status_idle;
    [ObservableProperty] public partial DateTime StatusTime { get; set; } = DateTime.Now;

    public bool CanRun()
    {
        return !IsBusy;
    }

    public async Task RunAsync(Func<DiagnosticReporter, IProgress<string>, CancellationToken, Task> action, CancellationToken? externalToken = null)
    {
        if (!CanRun()) return;
        var diagnostics = new DiagnosticReporter();
        IsBusy = true;

        var model = new DiagnosticsWindowViewModel();

        var progress = new Progress<string>(s =>
        {
            Status = s;
            StatusTime = DateTime.Now;
        });
        IProgress<string> ip = progress;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken ?? CancellationToken.None);

            ip.Report(Strings.Status_starting);
            await Task.Run(() => action(diagnostics, progress, cts.Token), cts.Token);
            ip.Report(Strings.Status_done);

            SystemSounds.Exclamation.Play();
        }
        catch (DiagnosticException ex)
        {
            diagnostics.Report(DiagnosticSeverity.Error, ex.Message, ex.Target);
            model.StackTrace = ex.ToString();
        }
        catch (Exception ex)
        {
            model.StackTrace = ex.ToString();
            diagnostics.Report(DiagnosticSeverity.Error, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }

        model.Diagnostics = [..diagnostics.Diagnostics];

        if (!diagnostics.HasProblems) return;
        if (diagnostics.HasErrors) ip.Report(Strings.Status_error);

        var window = new DiagnosticsWindow
        {
            DataContext = model,
            Owner = App.MainWindow
        };
        window.ShowDialog();
    }
}