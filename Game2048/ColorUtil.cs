using System.Windows.Media;

namespace Game2048
{
    public static partial class ColorUtil
    {
        public static string ColorToString(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static Color StringToColor(string colorString)
        {
            return (Color)ColorConverter.ConvertFromString(colorString);
        }

        public static Color BrushToColor(Brush brush)
        {
            return ((SolidColorBrush)brush).Color;
        }

        public static Color GetDarkerColor(Color lightColor, double factor = 0.7)
        {
            factor = Math.Clamp(factor, 0, 1);

            byte r = (byte)(lightColor.R * factor);
            byte g = (byte)(lightColor.G * factor);
            byte b = (byte)(lightColor.B * factor);

            return Color.FromArgb(lightColor.A, r, g, b);
        }
    }
}
