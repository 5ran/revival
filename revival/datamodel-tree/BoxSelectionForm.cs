using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataModelTreeViewer;

internal sealed class BoxSelectionForm : Form
{
    private Point start;
    private Rectangle selection;
    private bool dragging;

    public Rectangle SelectedScreenRect { get; private set; }

    public BoxSelectionForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        BackColor = Color.Black;
        Opacity = 0.15;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        KeyPreview = true;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        dragging = true;
        start = PointToScreen(e.Location);
        selection = new Rectangle(start, Size.Empty);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!dragging)
        {
            return;
        }

        var current = PointToScreen(e.Location);
        selection = Normalize(start, current);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!dragging)
        {
            return;
        }

        dragging = false;
        var current = PointToScreen(e.Location);
        SelectedScreenRect = Normalize(start, current);
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (selection.Width <= 0 || selection.Height <= 0)
        {
            return;
        }

        using var fill = new SolidBrush(Color.FromArgb(64, Color.DeepSkyBlue));
        using var pen = new Pen(Color.DeepSkyBlue, 2);
        e.Graphics.FillRectangle(fill, RectangleToClient(selection));
        e.Graphics.DrawRectangle(pen, RectangleToClient(selection));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static Rectangle Normalize(Point a, Point b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        var width = Math.Abs(a.X - b.X);
        var height = Math.Abs(a.Y - b.Y);
        return new Rectangle(x, y, width, height);
    }
}
