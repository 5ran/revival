using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace OpenMacroSwift.Desktop.Models;

public sealed partial class NavigationItem(string title, string pageKey, SymbolRegular symbol) : ObservableObject
{
    public string Title { get; } = title;
    public string PageKey { get; } = pageKey;
    public SymbolRegular Symbol { get; } = symbol;

    [ObservableProperty]
    private bool isSelected;
}

