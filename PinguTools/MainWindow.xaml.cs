using PinguTools.Common;
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
        Title = string.Format(Strings.Window_Title, Information.Name, Information.VersionString);

        Loaded += async (s, e) => await ((MainWindowViewModel)DataContext).UpdateCheck();
    }
}