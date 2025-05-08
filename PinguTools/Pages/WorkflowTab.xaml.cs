using Microsoft.Extensions.DependencyInjection;
using PinguTools.ViewModels;
using System.Windows.Controls;

namespace PinguTools.Pages;

public partial class WorkflowTab : UserControl
{
    public WorkflowTab()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<WorkflowTabViewModel>();
    }
}