using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace Client.Services;

internal static class ThemeContrast
{
    public static Color AdjustForReadableAccentText(Color color)
    {
        if (!IsLightTheme())
        {
            return color;
        }

        var luminance = RelativeLuminance(color);
        if (luminance <= 0.58)
        {
            return color;
        }

        var scale = 0.58 / luminance;
        return Color.FromArgb(
            color.A,
            (byte)System.Math.Clamp(System.Math.Round(color.R * scale), 0, 255),
            (byte)System.Math.Clamp(System.Math.Round(color.G * scale), 0, 255),
            (byte)System.Math.Clamp(System.Math.Round(color.B * scale), 0, 255));
    }

    private static bool IsLightTheme()
    {
        if (Application.Current?.ActualThemeVariant is { } variant)
        {
            return variant == ThemeVariant.Light;
        }

        return ThemeService.Current == AppTheme.Light;
    }

    private static double RelativeLuminance(Color color)
        => (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;
}
