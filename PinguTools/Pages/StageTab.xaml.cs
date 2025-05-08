using Microsoft.Extensions.DependencyInjection;
using PinguTools.ViewModels;
using System.Windows.Controls;

namespace PinguTools.Pages;

public partial class StageTab : UserControl
{
    public StageTab()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<StageTabViewModel>();
    }
}