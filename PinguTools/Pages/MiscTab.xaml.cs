using Microsoft.Extensions.DependencyInjection;
using PinguTools.ViewModels;
using System.Windows.Controls;

namespace PinguTools.Pages;

public partial class MiscTab : UserControl
{
    public MiscTab()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MiscViewModel>();
    }
}