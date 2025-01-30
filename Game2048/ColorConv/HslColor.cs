using System.Windows.Media;

namespace Game2048.ColorConv
{
    public static class HslColorExtension
    {
        public static string ToHslString(this Color color)
        {
            var (h, s, l) = color.ToHsl();
            return $"{h}, {s * 100}%, {l * 100}%";
        }

        public static (float hue, float saturation, float lightness) ToHsl(this Color color)
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

            if (h < 0) h += 360;

            float l = (cMax + cMin) / 2;

            float s = Δ switch
            {
                0 => 0,
                _ when l <= 0.5f => Δ / (cMax + cMin),
                _ => Δ / (2 - cMax - cMin)
            };

            return (h, s, l);
        }
    }

    public readonly struct HslColor
    {
        public readonly float hue;
        public readonly float saturation;
        public readonly float lightness;

        public readonly Color ToColor()
        {
            float c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            float x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
            float m = lightness - c / 2;

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

        public HslColor(float h, float s, float l)
        {
            (hue, saturation, lightness) = ColorParser.ClampValue((h, s, l));
        }

        public HslColor(string str)
        {
            (hue, saturation, lightness) = ColorParser.ParseString(str);
        }

        public static HslColor FromHsl(float h, float s, float l)
        {
            return new HslColor(h, s, l);
        }

        public static HslColor FromString(string str)
        {
            return new HslColor(str);
        }

        public override readonly string ToString()
        {
            return $"{hue}, {saturation * 100}%, {lightness * 100}%";
        }

        public static implicit operator Color(HslColor color)
        {
            return color.ToColor();
        }

    }
}
