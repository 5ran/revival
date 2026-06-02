using OpenMacroSwift.Desktop.Services;
using OpenMacroSwift.Desktop.ViewModels;
using System.Windows.Input;

namespace OpenMacroSwift.Desktop;

public partial class MainWindow
{
    private readonly WindowPlacementService placementService = new();
    private readonly MacroIpcClient ipcClient = new();

    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new ShellViewModel(ipcClient);
        DataContext = viewModel;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ShellViewModel.IsRebindingHotkey) && viewModel.IsRebindingHotkey)
            {
                Activate();
                Focus();
                Keyboard.Focus(this);
            }
        };
        placementService.Restore(this);

        Loaded += async (_, _) => await viewModel.InitializeAsync();
        PreviewKeyDown += OnPreviewKeyDown;
        Closing += (_, _) => placementService.Save(this);
        Closed += async (_, _) =>
        {
            viewModel.Shutdown();
            await ipcClient.DisposeAsync();
        };
    }

    private async void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (DataContext is not ShellViewModel { IsRebindingHotkey: true } viewModel)
        {
            return;
        }

        Key key = NormalizeKey(e);
        if (key == Key.Escape)
        {
            viewModel.CancelHotkeyRebind();
            e.Handled = true;
            return;
        }

        if (IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        string hotkey = FormatHotkey(key, Keyboard.Modifiers);
        await viewModel.CompleteHotkeyRebindAsync(hotkey);
        e.Handled = true;
    }

    private static Key NormalizeKey(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.System)
        {
            return e.SystemKey;
        }

        return e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key;
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin;
    }

    private static string FormatHotkey(Key key, ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");

        parts.Add(FormatKey(key));
        return string.Join("+", parts);
    }

    private static string FormatKey(Key key)
    {
        if (key >= Key.D0 && key <= Key.D9)
        {
            return ((char)('0' + key - Key.D0)).ToString();
        }

        if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            return $"Num{key - Key.NumPad0}";
        }

        return key switch
        {
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemBackslash => "\\",
            Key.Space => "Space",
            _ => key.ToString()
        };
    }
}

