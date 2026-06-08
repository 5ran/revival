using OpenMacroSwift.Desktop.Services;
using OpenMacroSwift.Desktop.ViewModels;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Input;

namespace OpenMacroSwift.Desktop;

public partial class MainWindow
{
    private const int WmHotkey = 0x0312;
    private const int ModAlt = 0x0001;
    private const int ModControl = 0x0002;
    private const int ModShift = 0x0004;
    private const int ModWin = 0x0008;

    private readonly WindowPlacementService placementService = new();
    private readonly MacroIpcClient ipcClient = new();
    private readonly Dictionary<int, string> registeredHotkeys = new();
    private HwndSource? hwndSource;
    private int nextHotkeyId = 1;

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

        Closing += (_, _) =>
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppContext.BaseDirectory, "revival_startup_error.log"),
                $"[{DateTime.Now:O}] mainwindow.closing\n");
            placementService.Save(this);
        };
        Closed += async (_, _) =>
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppContext.BaseDirectory, "revival_startup_error.log"),
                $"[{DateTime.Now:O}] mainwindow.closed\n");
            UnregisterHotkeys();
            viewModel.Shutdown();
            await ipcClient.DisposeAsync();
        };

        Loaded += async (_, _) =>
        {
            await viewModel.InitializeAsync();
            HookHotkeys(viewModel);
        };
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void HookHotkeys(ShellViewModel viewModel)
    {
        hwndSource ??= HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        hwndSource?.AddHook(WndProc);
        RegisterStartStopHotkey(viewModel.HotkeyText);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ShellViewModel.HotkeyText) && !viewModel.IsRebindingHotkey)
            {
                RegisterStartStopHotkey(viewModel.HotkeyText);
            }
        };
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey)
        {
            return IntPtr.Zero;
        }

        int id = wParam.ToInt32();
        if (!registeredHotkeys.ContainsKey(id))
        {
            return IntPtr.Zero;
        }

        handled = true;
        _ = Dispatcher.InvokeAsync(async () =>
        {
            if (DataContext is ShellViewModel viewModel)
            {
                await viewModel.ToggleMacroFromHotkeyAsync();
            }
        });
        return IntPtr.Zero;
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
        RegisterStartStopHotkey(hotkey);
        e.Handled = true;
    }

    private void RegisterStartStopHotkey(string hotkey)
    {
        UnregisterHotkeys();

        if (hwndSource is null || string.IsNullOrWhiteSpace(hotkey))
        {
            return;
        }

        if (!TryParseHotkey(hotkey, out uint modifiers, out uint virtualKey))
        {
            return;
        }

        int id = nextHotkeyId++;
        if (RegisterHotKey(hwndSource.Handle, id, modifiers, virtualKey))
        {
            registeredHotkeys[id] = hotkey;
        }
    }

    private void UnregisterHotkeys()
    {
        if (hwndSource is null)
        {
            registeredHotkeys.Clear();
            return;
        }

        foreach (int id in registeredHotkeys.Keys.ToArray())
        {
            UnregisterHotKey(hwndSource.Handle, id);
        }
        registeredHotkeys.Clear();
    }

    private static bool TryParseHotkey(string hotkey, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        foreach (string part in parts[..^1])
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                    modifiers |= ModControl;
                    break;
                case "ALT":
                    modifiers |= ModAlt;
                    break;
                case "SHIFT":
                    modifiers |= ModShift;
                    break;
                case "WIN":
                    modifiers |= ModWin;
                    break;
                default:
                    return false;
            }
        }

        virtualKey = ParseVirtualKey(parts[^1]);
        return virtualKey != 0;
    }

    private static uint ParseVirtualKey(string key)
    {
        return key.ToUpperInvariant() switch
        {
            "SPACE" => 0x20,
            "INSERT" => 0x2D,
            "DELETE" => 0x2E,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            _ when key.Length == 1 && key[0] is >= 'A' and <= 'Z' => (uint)key[0],
            _ when key.Length == 1 && key[0] is >= '0' and <= '9' => (uint)key[0],
            _ when key.StartsWith("NUM", StringComparison.OrdinalIgnoreCase) && key.Length == 4 && key[3] is >= '0' and <= '9' => (uint)(0x60 + (key[3] - '0')),
            _ when key.StartsWith("F", StringComparison.OrdinalIgnoreCase) && int.TryParse(key[1..], out int functionKey) && functionKey >= 1 && functionKey <= 24 => (uint)(0x70 + functionKey - 1),
            _ => 0
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

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

