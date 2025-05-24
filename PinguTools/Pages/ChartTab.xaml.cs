using Microsoft.Extensions.DependencyInjection;
using PinguTools.ViewModels;
using System.Windows.Controls;

namespace PinguTools.Pages;

public partial class ChartTab : UserControl
{
    public ChartTab()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ChartViewModel>();
    }
}