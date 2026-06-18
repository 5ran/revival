using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace Client.Views;

public partial class GeneralView : UserControl
{
    public GeneralView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void TrackingMethodDropdownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Popup>("TrackingMethodDropdownPopup") is { } popup)
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

    private void TrackingMethodList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: not null } &&
            this.FindControl<Popup>("TrackingMethodDropdownPopup") is { } popup)
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

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is StyledElement source)
        {
            for (var current = source; current is not null; current = current.Parent as StyledElement)
            {
                if (current is TextBox)
                {
                    return;
                }
            }
        }

        Focus(NavigationMethod.Pointer);
    }
}

