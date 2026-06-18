using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Client.Views;

public partial class AutoTotemView : UserControl
{
    public AutoTotemView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void StandaloneTotemDropdownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Popup>("StandaloneTotemDropdownPopup") is { } popup)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void StandaloneTotemList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: not null } &&
            this.FindControl<Popup>("StandaloneTotemDropdownPopup") is { } popup)
        {
            popup.IsOpen = false;
        }
    }
}

