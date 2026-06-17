using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataModelTreeViewer;

internal static class NodeHighlightOverlay
{
    private static HighlightWindow? window;

    public static void Show(GuiBounds? bounds, string? label = null)
    {
        if (bounds is null)
        {
            Hide();
            return;
        }

        if (window is null || window.IsDisposed)
        {
            window = new HighlightWindow();
        }

        window.ShowHighlight(bounds.Value, label);
    }

    public static void Hide()
    {
        if (window is null || window.IsDisposed)
        {
            return;
        }

        window.Hide();
    }
}

internal sealed class HighlightWindow : Form
{
    private GuiBounds bounds;
    private string? label;

    public HighlightWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;
        DoubleBuffered = true;
    }

    public void ShowHighlight(GuiBounds value, string? text)
    {
        bounds = value;
        label = text;
        var x = (int)Math.Round(value.X);
        var y = (int)Math.Round(value.Y);
        var width = Math.Max(1, (int)Math.Round(value.Width));
        var height = Math.Max(1, (int)Math.Round(value.Height));
        Bounds = new Rectangle(x - 2, y - 2, width + 4, height + 4);
        Show();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var rect = new Rectangle(2, 2, Math.Max(1, ClientSize.Width - 4), Math.Max(1, ClientSize.Height - 4));
        using var pen = new Pen(Color.DeepSkyBlue, 3);
        using var textBrush = new SolidBrush(Color.DeepSkyBlue);
        e.Graphics.DrawRectangle(pen, rect);
        if (!string.IsNullOrWhiteSpace(label))
        {
            e.Graphics.DrawString(label, SystemFonts.DefaultFont, textBrush, new PointF(4, 4));
        }
    }
}
