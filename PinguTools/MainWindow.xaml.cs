using PinguTools.Common;
using PinguTools.Misc;
using PinguTools.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace PinguTools;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        TypeDescriptor.AddAttributes(typeof(Difficulty), new TypeConverterAttribute(typeof(EnumDescriptionConverter)));
        TypeDescriptor.AddAttributes(typeof(StarDifficulty), new TypeConverterAttribute(typeof(EnumDescriptionConverter)));

        InitializeComponent();
        DataContext = viewModel;
        Title = $"{App.Name} v{App.Version}";

        Loaded += async (s, e) => await ((MainWindowViewModel)DataContext).UpdateCheck();
    }
}