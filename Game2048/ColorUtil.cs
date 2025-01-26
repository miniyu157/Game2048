using System.Windows.Media;

namespace Game2048
{
    public static class ColorUtil
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
            //限制 factor 在合理范围 [0, 1]
            factor = Math.Clamp(factor, 0, 1);

            //计算暗色
            byte r = (byte)(lightColor.R * factor);
            byte g = (byte)(lightColor.G * factor);
            byte b = (byte)(lightColor.B * factor);

            //返回新的颜色
            return Color.FromArgb(lightColor.A, r, g, b);
        }

        public static Color HslToRgb(float h, float s, float l)
        {
            h %= 360;
            s = Math.Clamp(s, 0, 1);
            l = Math.Clamp(l, 0, 1);

            float c = (1 - Math.Abs(2 * l - 1)) * s;
            float x = c * (1 - Math.Abs(h / 60 % 2 - 1));
            float m = l - c / 2;

            (float r0, float g0, float b0) = h switch
            {
                >= 0 and < 60 => (c, x, 0f),
                >= 60 and < 120 => (x, c, 0f),
                >= 120 and < 180 => (0f, c, x),
                >= 180 and < 240 => (0f, x, c),
                >= 240 and < 300 => (x, 0f, c),
                >= 300 and < 360 => (c, 0f, x),
                _ => (0, 0, 0)
            };

            byte r = (byte)((r0 + m) * 255);
            byte g = (byte)((g0 + m) * 255);
            byte b = (byte)((b0 + m) * 255);

            return Color.FromRgb(r, g, b);
        }
    }
}
