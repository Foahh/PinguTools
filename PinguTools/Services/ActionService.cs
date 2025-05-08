using CommunityToolkit.Mvvm.ComponentModel;
using PinguTools.Common;
using PinguTools.Controls;
using PinguTools.Localization;
using System.Media;

namespace PinguTools.Services;

public partial class ActionService : ObservableObject
{
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial string? Status { get; set; }

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

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken ?? CancellationToken.None);

            Status = Strings.Status_starting;
            var progress = new Progress<string>(s => Status = s);

            await Task.Run(() => action(diagnostics, progress, cts.Token), cts.Token);

            Status = Strings.Status_done;
            SystemSounds.Exclamation.Play();
        }
        catch (DiagnosticException ex)
        {
            model.StackTrace = ex.ToString();
            Status = Strings.Status_error;
        }
        catch (Exception ex)
        {
            model.StackTrace = ex.ToString();
            diagnostics.Report(DiagnosticSeverity.Error, ex.Message);
            Status = Strings.Status_error;
        }
        finally
        {
            IsBusy = false;
        }

        model.Diagnostics = [..diagnostics.Diagnostics];

        if (!diagnostics.HasProblems) return;
        if (diagnostics.HasErrors) Status = Strings.Status_error;
        var window = new DiagnosticsWindow
        {
            DataContext = model,
            Owner = App.MainWindow
        };
        window.ShowDialog();
    }
}