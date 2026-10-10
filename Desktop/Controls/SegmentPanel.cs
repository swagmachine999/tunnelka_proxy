using Avalonia;
using Avalonia.Controls;

namespace Tunnelka.Desktop;

public sealed class SegmentPanel : Panel
{
    private const double Gap = 6;
    private const double RowHeight = 34;

    private int _columns = 1;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0)
            return default;

        var cell = 0.0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            cell = Math.Max(cell, child.DesiredSize.Width);
        }

        _columns = ColumnsFor(availableSize.Width, cell);
        var rows = (Children.Count + _columns - 1) / _columns;
        var width = double.IsInfinity(availableSize.Width) ? _columns * cell + Gap * (_columns - 1) : availableSize.Width;
        return new Size(width, rows * RowHeight + Gap * (rows - 1));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var cellWidth = Math.Max(0, (finalSize.Width - Gap * (_columns - 1)) / _columns);
        for (var i = 0; i < Children.Count; i++)
        {
            var x = i % _columns * (cellWidth + Gap);
            var y = i / _columns * (RowHeight + Gap);
            Children[i].Arrange(new Rect(x, y, cellWidth, RowHeight));
        }

        return finalSize;
    }

    private int ColumnsFor(double width, double cell)
    {
        var count = Children.Count;
        if (double.IsInfinity(width))
            return count;

        var fit = (int)Math.Floor((width + Gap) / (cell + Gap));
        var columns = Math.Clamp(fit, 1, count);
        var rows = (int)Math.Ceiling(count / (double)columns);
        return (int)Math.Ceiling(count / (double)rows);
    }
}
