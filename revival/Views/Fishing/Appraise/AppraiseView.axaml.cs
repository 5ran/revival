using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Client.Views;

public partial class AppraiseView : UserControl
{
    public AppraiseView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void MutationSearchBox_OnGotFocus(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(OpenMutationOptions);
    }

    private void MutationSearchBox_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Dispatcher.UIThread.Post(OpenMutationOptions);
    }

    private void MutationDropdownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenMutationOptions();
    }

    private void MutationSearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox { IsFocused: true })
        {
            Dispatcher.UIThread.Post(OpenMutationOptions);
        }
    }

    private void OpenMutationOptions()
    {
        if (this.FindControl<Popup>("MutationDropdownPopup") is { } popup)
        {
            popup.IsOpen = true;
        }
    }
}
