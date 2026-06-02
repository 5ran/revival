using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace OpenMacroSwift.Desktop.Models;

public sealed partial class MacroModule : ObservableObject
{
    public MacroModule(string title, string description, SymbolRegular symbol)
    {
        Title = title;
        Description = description;
        Symbol = symbol;
    }

    public string Title { get; }
    public string Description { get; }
    public SymbolRegular Symbol { get; }

    [ObservableProperty]
    private bool isEnabled;

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private string status = "Idle";
}

