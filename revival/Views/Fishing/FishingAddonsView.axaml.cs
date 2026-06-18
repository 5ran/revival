using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Client.ViewModels;

namespace Client.Views;

public partial class FishingAddonsView : UserControl
{
    public FishingAddonsView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void PrimaryTotemDropdownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Popup>("PrimaryTotemDropdownPopup") is { } popup)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void SecondaryTotemDropdownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Popup>("SecondaryTotemDropdownPopup") is { } popup)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void PrimaryTotemList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not FishingAddonsViewModel || sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not null &&
            this.FindControl<Popup>("PrimaryTotemDropdownPopup") is { } popup)
        {
            popup.IsOpen = false;
        }
    }

    private void SecondaryTotemList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not FishingAddonsViewModel || sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not null &&
            this.FindControl<Popup>("SecondaryTotemDropdownPopup") is { } popup)
        {
            popup.IsOpen = false;
        }
    }
}
