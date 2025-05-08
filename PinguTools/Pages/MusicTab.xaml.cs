using Microsoft.Extensions.DependencyInjection;
using PinguTools.ViewModels;
using System.Windows.Controls;

namespace PinguTools.Pages;

public partial class MusicTab : UserControl
{
    public MusicTab()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MusicTabViewModel>();
    }
}