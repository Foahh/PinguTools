using PinguTools.Resources;
using PinguTools.ViewModels;
using System.Windows;

namespace PinguTools;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Title = string.Format(Strings.Window_Title, App.Name, App.Version.ToString(3));

        Loaded += async (s, e) => await ((MainWindowViewModel)DataContext).UpdateCheck();
    }
}