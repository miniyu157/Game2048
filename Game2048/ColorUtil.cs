using System.Text.RegularExpressions;
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

        /// <summary>
        /// 指定的颜色格式。
        /// </summary>
        public enum ColorFormat
        {
            Hsv,
            Hsl
        }

        /// <summary>
        /// 根据 HSL 值创建 <see cref="Color"/> 对象。
        /// </summary>
        /// <param name="h">色相，范围为 0 到 360。</param>
        /// <param name="s">饱和度，范围为 0 到 1。</param>
        /// <param name="l">亮度，范围为 0 到 1。</param>
        /// <returns>表示指定 HSL 值的 <see cref="Color"/> 对象。</returns>
        public static Color ConvertFromHSL(float h, float s, float l)
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

        /// <summary>
        /// 根据 HSV 值创建 <see cref="Color"/> 对象。
        /// </summary>
        /// <param name="h">色相，范围为 0 到 360。</param>
        /// <param name="s">饱和度，范围为 0 到 1。</param>
        /// <param name="v">亮度，范围为 0 到 1。</param>
        /// <returns>表示指定 HSV 值的 <see cref="Color"/> 对象。</returns>
        public static Color ConvertFromHSV(float h, float s, float v)
        {
            h %= 360;
            s = Math.Clamp(s, 0, 1);
            v = Math.Clamp(v, 0, 1);

            float c = v * s;
            float x = c * (1 - Math.Abs(h / 60 % 2 - 1));
            float m = v - c;

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

        /// <summary>
        /// 将形如 xxx(xx, xx, xx) 格式颜色的字符串转换为 <see cref="Color"/>。
        /// </summary>
        /// <remarks>例如 hsv(123.456, 52.25%, 42.56%)
        /// <br/>
        /// hsl(123.456, 0.5225, 0.4256)
        /// </remarks>
        /// <param name="str">要转换的原字符串。</param>
        public static Color ConvertFromString(string str)
        {
            static float ParsePercentage(string input) =>
                input.EndsWith('%')
                    ? float.Parse(input.TrimEnd('%')) / 100
                    : float.Parse(input);

            if (string.IsNullOrWhiteSpace(str))
                return default;

            string s = str.Trim();

            Match match = ColorFormatRegex().Match(s);

            if (match.Success)
            {
                string head = match.Groups[1].Value;
                string content = match.Groups[2].Value;

                ColorFormat format = head.ToLower() switch
                {
                    "hsv" => ColorFormat.Hsv,
                    "hsl" => ColorFormat.Hsl,
                    _ => throw new NotSupportedException($"暂不支持格式 '{head}' 与 {typeof(Color)} 之间进行转换。")
                };

                string[] parts = content.Split(',');

                if (parts.Length != 3) throw new ArgumentException($"{format} 必须由三个参数组成。");

                string p1 = parts[0].Trim();
                string p2 = parts[1].Trim();
                string p3 = parts[2].Trim();

                float hue = float.TryParse(p1, out float h) ? h : 0;
                float saturation = ParsePercentage(p2);
                float value = ParsePercentage(p3);

                return format switch
                {
                    ColorFormat.Hsv => ConvertFromHSV(hue, saturation, value),
                    ColorFormat.Hsl => ConvertFromHSL(hue, saturation, value),
                    _ => default
                };
            }

            throw new ArgumentException($"格式无效。");
        }


        /// <summary>
        /// 将 <see cref="Color"/> 对象转换为 HSV 值。
        /// </summary>
        /// <param name="color">要转换的 <see cref="Color"/> 对象。</param>
        /// <returns>表示 HSV 值的元组 (H, S, V)。</returns>
        public static (float, float, float) ToHsv(this Color color)
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

        /// <summary>
        /// 将 <see cref="Color"/> 对象转换为 HSL 值。
        /// </summary>
        /// <param name="color">要转换的 <see cref="Color"/> 对象。</param>
        /// <returns>表示 HSL 值的元组 (H, S, L)。</returns>
        public static (float, float, float) ToHsl(this Color color)
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

        /// <summary>
        /// 将 <see cref="Color"/> 转换为形如 xxx(xx, xx, xx) 格式的字符串。
        /// </summary>
        /// <param name="color">要转换的 <see cref="Color"/>。</param>
        /// <param name="colorFormat">指定的颜色格式，使用 <see cref="ColorFormat"/> 枚举值表示。</param>
        /// <param name="isPercentage">是否除了色相以外全部使用百分比，默认为 false。</param>
        public static string ToColorString(this Color color, ColorFormat colorFormat, bool isPercentage = false)
        {
            if (!isPercentage)
            {
                return colorFormat.ToString().ToLower() + (colorFormat switch
                {
                    ColorFormat.Hsv => color.ToHsv().ToString(),
                    ColorFormat.Hsl => color.ToHsl().ToString(),
                    _ => string.Empty
                });
            }
            else
            {
                string content = string.Empty;
                switch (colorFormat)
                {
                    case ColorFormat.Hsv:
                        var hsv = color.ToHsv();
                        content = $"({hsv.Item1}, {hsv.Item2 * 100}%, {hsv.Item3 * 100}%)";
                        break;

                    case ColorFormat.Hsl:
                        var hsl = color.ToHsl();
                        content = $"({hsl.Item1}, {hsl.Item2 * 100}%, {hsl.Item3 * 100}%)";
                        break;
                }
                return colorFormat.ToString().ToLower() + content;
            }
        }

        [GeneratedRegex(@"^(.*)\((.*)\)$", RegexOptions.IgnoreCase, "zh-CN")]
        private static partial Regex ColorFormatRegex();
    }
}
