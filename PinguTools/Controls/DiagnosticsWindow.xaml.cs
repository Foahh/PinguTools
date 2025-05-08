/*
   This code is originally from https://github.com/paralleltree/Ched
*/

using CommunityToolkit.Mvvm.ComponentModel;
using PinguTools.Common;
using PinguTools.Localization;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PinguTools.Controls;

public partial class DiagnosticsWindow
{
    public DiagnosticsWindow()
    {
        InitializeComponent();
    }

    private void OnShowDetailsClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Parent is not Grid grid) return;
        if (grid.FindName("ObjectTreeViewPopup") is not Popup popup) return;
        popup.IsOpen = true;
    }
}

public partial class DiagnosticsWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = Strings.Title_Diagnostics;

    [ObservableProperty]
    public partial ObservableCollection<Diagnostic>? Diagnostics { get; set; }

    [ObservableProperty]
    public partial string? StackTrace { get; set; } = null;
}