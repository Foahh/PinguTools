/*
   This code is modified from https://github.com/paralleltree/Ched
   Original Author: paralleltree
*/

using CommunityToolkit.Mvvm.ComponentModel;
using PinguTools.Common;
using PinguTools.Localization;
using PinguTools.Misc;

namespace PinguTools.Controls;

public partial class DiagnosticsWindow
{
    public DiagnosticsWindow()
    {
        InitializeComponent();
    }
}

public partial class DiagnosticsWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = Strings.Title_Diagnostics;

    [ObservableProperty]
    public partial ObservableSortedSet<Diagnostic>? Diagnostics { get; set; }

    [ObservableProperty]
    public partial string? StackTrace { get; set; } = null;

    [ObservableProperty]
    public partial Diagnostic? SelectedDiagnostic { get; set; } = null;
}