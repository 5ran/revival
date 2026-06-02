using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MotionPathProbe;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly TextBox _pathBox = new() { Dock = DockStyle.Top, Height = 24 };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 24, Text = "Status: idle" };
    private readonly Label _coords = new() { Dock = DockStyle.Top, Height = 24, Text = "Rel: -" };
    private readonly Label _moving = new() { Dock = DockStyle.Top, Height = 32, Text = "MOVING: NO", Font = new Font("Segoe UI", 12, FontStyle.Bold) };
    private readonly TextBox _childrenBox = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 50 };

    private readonly RobloxProbe _probe = new();
    private (float X, float Y)? _lastRel;
    private readonly Dictionary<ulong, (float X, float Y)> _lastChildRel = new();

    public MainForm()
    {
        Text = "Motion Path Probe (Hold R)";
        Width = 820;
        Height = 320;
        TopMost = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var hint = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = "Paste full DataModel path. Hold R to sample motion.",
        };

        _pathBox.Text = "DataModel/Players:Players/Player:roblox_user_5569660191/PlayerGui:PlayerGui/ScreenGui:reel/Frame:bar/Frame:Details";
        Controls.Add(_childrenBox);
        Controls.Add(_moving);
        Controls.Add(_coords);
        Controls.Add(_status);
        Controls.Add(_pathBox);
        Controls.Add(hint);

        _timer.Tick += (_, _) => TickProbe();
        _timer.Start();
    }

    private void TickProbe()
    {
        try
        {
            if (!Win32.IsKeyDown(Keys.R))
            {
                _status.Text = "Status: waiting for R key";
                return;
            }

            var parts = ParsePath(_pathBox.Text);
            if (parts.Count < 2)
            {
                _status.Text = "Status: invalid path";
                return;
            }

            var sample = _probe.SampleRelative(parts);
            if (!sample.Found)
            {
                _status.Text = "Status: target not found";
                _moving.Text = "MOVING: NO";
                _moving.ForeColor = Color.DarkRed;
                return;
            }

            _status.Text = $"Status: found parent=0x{sample.Parent:X} child=0x{sample.Child:X}";
            _coords.Text = $"Rel: X={sample.RelX:0.0000}, Y={sample.RelY:0.0000}";

            var moving = false;
            if (_lastRel is { } last)
            {
                var dx = Math.Abs(sample.RelX - last.X);
                var dy = Math.Abs(sample.RelY - last.Y);
                moving = dx > 0.0007f || dy > 0.0007f;
            }

            _lastRel = (sample.RelX, sample.RelY);
            _moving.Text = moving ? "MOVING: YES" : "MOVING: NO";
            _moving.ForeColor = moving ? Color.DarkGreen : Color.DarkRed;

            var children = _probe.SampleDescendantsRelative(parts);
            var sb = new StringBuilder();
            var anyMovingChild = false;
            var seen = new HashSet<ulong>();
            foreach (var c in children)
            {
                seen.Add(c.Address);
                var childMoving = false;
                if (_lastChildRel.TryGetValue(c.Address, out var prev))
                {
                    childMoving = Math.Abs(c.RelX - prev.X) > 0.0007f || Math.Abs(c.RelY - prev.Y) > 0.0007f;
                }

                _lastChildRel[c.Address] = (c.RelX, c.RelY);
                if (!childMoving)
                {
                    continue;
                }

                anyMovingChild = true;
                sb.AppendLine($"[MOVE] {c.Name} 0x{c.Address:X} rel=({c.RelX:0.0000},{c.RelY:0.0000})");
            }

            foreach (var key in _lastChildRel.Keys.ToArray())
            {
                if (!seen.Contains(key))
                {
                    _lastChildRel.Remove(key);
                }
            }

            _childrenBox.Text = sb.Length == 0 ? $"No moving descendants. Scanned: {children.Count}" : $"Moving descendants (scanned {children.Count}):{Environment.NewLine}{sb}";
            if (anyMovingChild)
            {
                _moving.Text = "MOVING: YES (child scan)";
                _moving.ForeColor = Color.DarkGreen;
            }
            else
            {
                _moving.Text = "MOVING: NO";
                _moving.ForeColor = Color.DarkRed;
            }
        }
        catch (Exception ex)
        {
            _status.Text = "Status: error " + ex.Message;
            _moving.Text = "MOVING: NO";
            _moving.ForeColor = Color.DarkRed;
        }
    }

    private static List<string> ParsePath(string raw)
    {
        var result = new List<string>();
        foreach (var segment in raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var s = segment;
            var colon = s.IndexOf(':');
            if (colon >= 0 && colon + 1 < s.Length)
            {
                s = s[(colon + 1)..];
            }

            var bracket = s.IndexOf('[');
            if (bracket > 0)
            {
                s = s[..bracket];
            }

            s = s.Trim();
            if (s.Length > 0)
            {
                result.Add(s);
            }
        }

        if (result.Count > 0 && string.Equals(result[0], "DataModel", StringComparison.OrdinalIgnoreCase))
        {
            result.RemoveAt(0);
        }

        return result;
    }
}

internal sealed class RobloxProbe
{
    private readonly Memory _mem = new();

    public (bool Found, ulong Parent, ulong Child, float RelX, float RelY) SampleRelative(IReadOnlyList<string> pathParts)
    {
        _mem.Attach();

        var dataModel = _mem.GetDataModel();
        if (dataModel == 0) return default;

        ulong current = dataModel;
        for (int i = 0; i < pathParts.Count; i++)
        {
            var name = pathParts[i];
            var next = _mem.FindChildByName(current, name);
            if (next == 0 && name.StartsWith("Masterline", StringComparison.OrdinalIgnoreCase))
            {
                next = _mem.FindChildByPrefix(current, "Masterline");
            }

            if (next == 0) return default;
            current = next;
        }

        var child = current;
        var parent = _mem.ReadPtr(child + Offsets.InstanceParent);
        if (parent == 0) return default;

        var c = _mem.ReadAbsPosition(child);
        var p = _mem.ReadAbsPosition(parent);

        return (true, parent, child, c.X - p.X, c.Y - p.Y);
    }

    public IReadOnlyList<ChildRelativeSample> SampleDescendantsRelative(IReadOnlyList<string> pathParts)
    {
        _mem.Attach();

        var dataModel = _mem.GetDataModel();
        if (dataModel == 0) return Array.Empty<ChildRelativeSample>();

        ulong current = dataModel;
        for (int i = 0; i < pathParts.Count; i++)
        {
            var name = pathParts[i];
            var next = _mem.FindChildByName(current, name);
            if (next == 0 && name.StartsWith("Masterline", StringComparison.OrdinalIgnoreCase))
            {
                next = _mem.FindChildByPrefix(current, "Masterline");
            }

            if (next == 0) return Array.Empty<ChildRelativeSample>();
            current = next;
        }

        var parentPos = _mem.ReadAbsPosition(current);
        var values = new List<ChildRelativeSample>();
        foreach (var child in _mem.ReadDescendants(current))
        {
            var name = _mem.ReadName(child);
            var pos = _mem.ReadAbsPosition(child);
            values.Add(new ChildRelativeSample(child, name, pos.X - parentPos.X, pos.Y - parentPos.Y));
        }

        return values;
    }
}

internal readonly record struct ChildRelativeSample(ulong Address, string Name, float RelX, float RelY);

internal static class Offsets
{
    public const ulong FakeDataModelPointer = 0x74f6758;
    public const ulong FakeDataModelToDataModel = 0x1d0;
    public const ulong InstanceChildrenStart = 0x78;
    public const ulong InstanceChildrenEnd = 0x8;
    public const ulong InstanceName = 0xb0;
    public const ulong InstanceParent = 0x70;
    public const ulong MiscStringLength = 0x10;
    public const ulong GuiAbsolutePosition = 0x110;
}

internal sealed class Memory
{
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private IntPtr _handle;
    private Process? _proc;
    private ulong _base;

    public void Attach()
    {
        if (_proc is { HasExited: false } && _handle != IntPtr.Zero) return;

        _proc = Process.GetProcessesByName("RobloxPlayerBeta").OrderByDescending(p => p.StartTime).FirstOrDefault()
            ?? Process.GetProcesses().FirstOrDefault(p => p.ProcessName.Contains("Roblox", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Roblox process not found");

        _base = unchecked((ulong)_proc.MainModule!.BaseAddress.ToInt64());
        _handle = Win32.OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, _proc.Id);
        if (_handle == IntPtr.Zero) throw new InvalidOperationException("OpenProcess failed");
    }

    public ulong GetDataModel()
    {
        var fake = ReadPtr(_base + Offsets.FakeDataModelPointer);
        return fake == 0 ? 0 : ReadPtr(fake + Offsets.FakeDataModelToDataModel);
    }

    public ulong FindChildByName(ulong parent, string target)
    {
        foreach (var child in ReadChildren(parent))
        {
            if (string.Equals(ReadName(child), target, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return 0;
    }

    public ulong FindChildByPrefix(ulong parent, string prefix)
    {
        foreach (var child in ReadChildren(parent))
        {
            var n = ReadName(child);
            if (n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return 0;
    }

    public IEnumerable<ulong> ReadChildren(ulong instance)
    {
        var listPtr = ReadPtr(instance + Offsets.InstanceChildrenStart);
        var start = ReadPtr(listPtr);
        var end = ReadPtr(listPtr + Offsets.InstanceChildrenEnd);
        if (start == 0 || end == 0 || end < start || end - start > 0x100000) yield break;

        for (var e = start; e < end; e += 0x10)
        {
            var child = ReadPtr(e);
            if (child > 0x10000) yield return child;
        }
    }

    public IEnumerable<ulong> ReadDescendants(ulong root)
    {
        var queue = new Queue<ulong>();
        var seen = new HashSet<ulong>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            foreach (var child in ReadChildren(node))
            {
                if (!seen.Add(child))
                {
                    continue;
                }

                yield return child;
                queue.Enqueue(child);
            }
        }
    }

    public string ReadName(ulong instance)
    {
        return ReadString(ReadPtr(instance + Offsets.InstanceName));
    }

    public (float X, float Y) ReadAbsPosition(ulong instance)
    {
        var x = ReadFloat(instance + Offsets.GuiAbsolutePosition);
        var y = ReadFloat(instance + Offsets.GuiAbsolutePosition + 0x4);
        return (x, y);
    }

    public ulong ReadPtr(ulong address)
    {
        var b = ReadBytes(address, IntPtr.Size);
        if (b is null) return 0;
        return IntPtr.Size == 8 ? BitConverter.ToUInt64(b, 0) : BitConverter.ToUInt32(b, 0);
    }

    private int ReadInt32(ulong address)
    {
        var b = ReadBytes(address, 4);
        return b is null ? 0 : BitConverter.ToInt32(b, 0);
    }

    private float ReadFloat(ulong address)
    {
        var b = ReadBytes(address, 4);
        return b is null ? 0 : BitConverter.ToSingle(b, 0);
    }

    private string ReadString(ulong address)
    {
        if (address < 0x10000) return string.Empty;
        var len = ReadInt32(address + Offsets.MiscStringLength);
        if (len <= 0 || len > 1000) return string.Empty;
        var data = len > 15 ? ReadPtr(address) : address;
        var b = ReadBytes(data, len);
        return b is null ? string.Empty : Encoding.UTF8.GetString(b);
    }

    private byte[]? ReadBytes(ulong address, int count)
    {
        if (_handle == IntPtr.Zero || address < 0x10000 || count <= 0) return null;
        var buffer = new byte[count];
        return Win32.ReadProcessMemory(_handle, new IntPtr(unchecked((long)address)), buffer, count, out var read) && read.ToInt64() == count
            ? buffer
            : null;
    }
}

internal static class Win32
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr process, IntPtr baseAddress, byte[] buffer, int size, out IntPtr bytesRead);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public static bool IsKeyDown(Keys key) => (GetAsyncKeyState((int)key) & 0x8000) != 0;
}
