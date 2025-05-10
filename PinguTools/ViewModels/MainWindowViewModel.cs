using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PinguTools.Localization;
using PinguTools.Services;
using System.Diagnostics;

namespace PinguTools.ViewModels;

public partial class MainWindowViewModel : ViewModel
{
    private readonly ActionService actions;
    private readonly IUpdateService updateService;

    public MainWindowViewModel(ActionService acs, IUpdateService updateService) : base(acs)
    {
        actions = acs;
        this.updateService = updateService;
        actions.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);
    }

    public bool IsUpdateAvailable => LatestVersion != null && LatestVersion > App.Version;

    [ObservableProperty]
    public partial string? DownloadUrl { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadUpdateCommand))]
    public partial Version? LatestVersion { get; set; }

    [ObservableProperty]
    public partial string UpdateStatus { get; set; } = string.Empty;

    public string Status => actions.Status;
    public DateTime StatusTime => actions.StatusTime;

    [RelayCommand]
    public async Task UpdateCheck()
    {
        UpdateStatus = Strings.UpdateCheck_Checking;
        try
        {
            var (result, url) = await updateService.CheckForUpdatesAsync();
            LatestVersion = result;
            DownloadUrl = url;
            UpdateStatus = IsUpdateAvailable ? string.Format(Strings.UpdateCheck_New_Version_Available, LatestVersion.ToString(3)) : Strings.UpdateCheck_Already_Latest;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            UpdateStatus = Strings.UpdateCheck_Failed;
            LatestVersion = null;
            DownloadUrl = null;
        }
    }

    [RelayCommand(CanExecute = nameof(IsUpdateAvailable))]
    public void DownloadUpdate()
    {
        if (string.IsNullOrEmpty(DownloadUrl)) return;
        Process.Start(new ProcessStartInfo
        {
            FileName = DownloadUrl,
            UseShellExecute = true
        });
    }
}