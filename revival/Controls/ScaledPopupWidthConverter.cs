using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Client.Controls;

public sealed class ScaledPopupWidthConverter : IValueConverter
{
    public double Scale { get; set; } = 1.0;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            return width * Scale;
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width && Scale != 0)
        {
            return width / Scale;
        }

        return value;
    }
}
