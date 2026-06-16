using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Client.Views;

public partial class EnchantView : UserControl
{
    public EnchantView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EnchantDropdownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Popup>("EnchantDropdownPopup") is { } popup)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void EnchantTargetList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.FindControl<Popup>("EnchantDropdownPopup") is { } popup)
        {
            popup.IsOpen = false;
        }
    }
}
