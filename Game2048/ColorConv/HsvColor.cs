using System.Windows.Media;

namespace Game2048.ColorConv
{
    public static class HsvColorExtension
    {
        public static string ToHsvString(this Color color)
        {
            var (h, s, v) = color.ToHsv();
            return $"{h}, {s * 100}%, {v * 100}%";
        }

        public static (float hue, float saturation, float value) ToHsv(this Color color)
        {
            float r0 = (float)color.R / 255;
            float g0 = (float)color.G / 255;
            float b0 = (float)color.B / 255;

            float cMax = Math.Max(r0, Math.Max(g0, b0));
            float cMin = Math.Min(r0, Math.Min(g0, b0));
            float Δ = cMax - cMin;

            float h = Δ switch
            {
                0 => 0,
                _ when cMax == r0 => 60 * ((g0 - b0) / Δ % 6),
                _ when cMax == g0 => 60 * ((b0 - r0) / Δ + 2),
                _ when cMax == b0 => 60 * ((r0 - g0) / Δ + 4),
                _ => 0
            };

            float s = cMax switch
            {
                0 => 0,
                _ => Δ / cMax
            };

            float v = cMax;

            if (h < 0) h += 360;
            return (h, s, v);
        }
    }

    public readonly struct HsvColor
    {
        public readonly float hue;
        public readonly float saturation;
        public readonly float value;

        public readonly Color ToColor()
        {
            float c = value * saturation;
            float x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
            float m = value - c;

            (float r0, float g0, float b0) = hue switch
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

        public HsvColor(float h, float s, float v)
        {
            (hue, saturation, value) = ColorParser.ClampValue((h, s, v));
        }

        public HsvColor(string str)
        {
            (hue, saturation, value) = ColorParser.ParseString(str);
        }

        public static HsvColor FromHsv(float h, float s, float v)
        {
            return new HsvColor(h, s, v);
        }

        public static HsvColor FromString(string str)
        {
            return new HsvColor(str);
        }

        public override readonly string ToString()
        {
            return $"{hue}, {saturation * 100}%, {value * 100}%";
        }

        public static implicit operator Color(HsvColor color)
        {
            return color.ToColor();
        }

    }
}
