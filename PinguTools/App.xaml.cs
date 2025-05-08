using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PinguTools.Audio;
using PinguTools.Chart;
using PinguTools.Controls;
using PinguTools.Image;
using PinguTools.Properties;
using PinguTools.Services;
using PinguTools.ViewModels;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace PinguTools;

public partial class App : Application
{
    public static readonly string Name = Assembly.GetEntryAssembly()?.GetName().Name ?? throw new InvalidOperationException("Failed to retrieve application name");
    public static readonly Version Version = Assembly.GetEntryAssembly()?.GetName().Version ?? throw new InvalidOperationException("Failed to retrieve application version");
    public static readonly string VersionString = Version.ToString(3);
    public static readonly DateTime BuildDate = BuildDateAttribute.GetAssemblyBuildDate();

    private IHost host = null!;
    internal new static Window MainWindow => Services.GetRequiredService<MainWindow>();
    internal static string TempDir => Path.Combine(Path.GetTempPath(), Name);
    internal static IServiceProvider Services => ((App)Current).host.Services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var basePath = Path.GetDirectoryName(AppContext.BaseDirectory);
        if (basePath != null) Directory.SetCurrentDirectory(basePath);


        host = Host.CreateDefaultBuilder().ConfigureServices((_, services) =>
        {
            services.AddSingleton<MainWindow>();

            services.AddSingleton<ActionService>();
            services.AddSingleton<ResourceService>(_ =>
            {
                var res = new ResourceService(TempDir);
                res.Register("ebur128.dll", EmbeddedResources.ebur128_dll);
                return res;
            });
            services.AddSingleton<AssetService>(provider =>
            {
                var res = provider.GetRequiredService<ResourceService>();
                res.Register("solid_assets.json", EmbeddedResources.assets_json);
                var solidAssetPath = res.GetRegisteredPath("solid_assets.json");
                return new AssetService(solidAssetPath, "assets.json");
            });

            services.AddSingleton<IUpdateService, GitHubUpdateService>();

            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<WorkflowTabViewModel>();
            services.AddTransient<ChartTabViewModel>();
            services.AddTransient<JacketTabViewModel>();
            services.AddTransient<MusicTabViewModel>();
            services.AddTransient<StageTabViewModel>();
            services.AddTransient<MiscTabViewModel>();

            services.AddTransient<LoudNormalizer>();
            services.AddTransient<DdsChunkLocator>();
            services.AddTransient<AfbExtractor>();
            services.AddTransient<JacketConverter>();
            services.AddTransient<StageConverter>();
            services.AddTransient<MusicConverter>();
            services.AddTransient<MgxcParser>(provider =>
            {
                var res = provider.GetRequiredService<AssetService>();
                return new MgxcParser(res.WeTagNames);
            });
            services.AddTransient<ChartConverter>();
        }).Build();
        host.Start();

        var window = Services.GetRequiredService<MainWindow>();
        window.Show();

        DispatcherUnhandledException += (s, ex) =>
        {
            if (ex.Exception is OperationCanceledException opex)
            {
                ex.Handled = true;
            }

            var errorWindow = new ExceptionWindow
            {
                StackTrace = ex.Exception.ToString()
            };
            errorWindow.ShowDialog();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        host.Dispose();
    }
}