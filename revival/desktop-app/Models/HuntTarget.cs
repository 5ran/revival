using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenMacroSwift.Desktop.Models;

public sealed partial class HuntTarget(string name) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty]
    private bool isSelected;
}

