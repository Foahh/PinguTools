using Microsoft.Extensions.DependencyInjection;
using PinguTools.ViewModels;
using System.Windows.Controls;

namespace PinguTools.Pages;

public partial class JacketTab : UserControl
{
    public JacketTab()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<JacketViewModel>();
    }
}