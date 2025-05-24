using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PinguTools.Common;
using PinguTools.Controls;
using PinguTools.Services;
using PinguTools.ViewModels;
using System.IO;
using System.Text;
using System.Windows;

namespace PinguTools;

public partial class App : Application
{
    private IHost host = null!;
    internal new static Window MainWindow => Services.GetRequiredService<MainWindow>();
    internal static IServiceProvider Services => ((App)Current).host.Services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var basePath = Path.GetDirectoryName(AppContext.BaseDirectory);
        if (basePath != null) Directory.SetCurrentDirectory(basePath);

        ResourceManager.Initialize();

        host = Host.CreateDefaultBuilder().ConfigureServices((_, services) =>
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<ActionService>();
            services.AddSingleton<AssetManager>();
            services.AddSingleton<IUpdateService, GitHubUpdateService>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<WorkflowViewModel>();
            services.AddTransient<ChartViewModel>();
            services.AddTransient<JacketViewModel>();
            services.AddTransient<MusicViewModel>();
            services.AddTransient<StageViewModel>();
            services.AddTransient<MiscViewModel>();
            services.AddTransient<OptionViewModel>();
        }).Build();

        host.Start();

        var window = Services.GetRequiredService<MainWindow>();
        window.Show();

        DispatcherUnhandledException += (s, ex) =>
        {
            var errorWindow = new ExceptionWindow { StackTrace = ex.Exception.ToString() };
            errorWindow.ShowDialog();
            if (ex.Exception is OperationCanceledException or DiagnosticException) ex.Handled = true;
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ResourceManager.Release();
        host.Dispose();
    }
}