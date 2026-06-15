using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Client.Views;

public partial class TraderView : UserControl
{
    public TraderView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
