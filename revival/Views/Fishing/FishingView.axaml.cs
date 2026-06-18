using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Client.Views;

public partial class FishingView : UserControl
{
    public FishingView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void TrackerDropdownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Popup>("TrackerDropdownPopup") is { } popup)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void CastingDropdownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Popup>("CastingDropdownPopup") is { } popup)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void TrackerList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: not null } &&
            this.FindControl<Popup>("TrackerDropdownPopup") is { } popup)
        {
            popup.IsOpen = false;
        }
    }

    private void CastingList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: not null } &&
            this.FindControl<Popup>("CastingDropdownPopup") is { } popup)
        {
            popup.IsOpen = false;
        }
    }
}
