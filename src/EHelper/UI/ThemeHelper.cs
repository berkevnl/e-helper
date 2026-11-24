using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace EHelper.UI
{
    /// <summary>
    /// Shared theme palette and color helpers used by every window.
    /// </summary>
    public static class ThemeHelper
    {
        public static readonly MediaColor DefaultAccent = MediaColor.FromRgb(34, 211, 238); // #22D3EE

        // Brushes mirrored into Application resources so popups/dropdowns render themed
        private static readonly string[] ApplicationKeys =
        {
            "CardBg", "CardHoverBg", "CardBorder", "CardActiveBorder", "CardActiveBg",
            "TextPrimary", "TextSecondary", "TextMuted", "AccentColor"
        };

        /// <summary>
        /// Applies the base palette common to all windows and returns the parsed accent color.
        /// </summary>
        public static MediaColor ApplyBasePalette(ResourceDictionary res, bool isDark, string? accentHex)
        {
            var accent = ParseColor(accentHex, DefaultAccent);

            Set(res, "WindowBg", isDark ? Rgb(20, 21, 27) : Rgb(248, 250, 252));
            Set(res, "CardBg", isDark ? Rgb(28, 30, 38) : Rgb(255, 255, 255));
            Set(res, "CardHoverBg", isDark ? Rgb(34, 37, 48) : Rgb(241, 245, 249));
            Set(res, "CardBorder", isDark ? Rgb(37, 40, 52) : Rgb(226, 232, 240));
            Set(res, "CardActiveBg", WithAlpha(accent, isDark ? (byte)50 : (byte)30));
            Set(res, "CardActiveBorder", accent);
            Set(res, "TextPrimary", isDark ? Rgb(255, 255, 255) : Rgb(15, 23, 42));
            Set(res, "TextSecondary", isDark ? Rgb(212, 215, 226) : Rgb(51, 65, 85));
            Set(res, "TextMuted", isDark ? Rgb(101, 106, 123) : Rgb(100, 116, 139));
            Set(res, "AccentColor", accent);
            Set(res, "BadgeBg", isDark ? Rgb(34, 36, 47) : Rgb(241, 245, 249));
            Set(res, "BadgeBorder", isDark ? Rgb(45, 49, 63) : Rgb(203, 213, 225));

            return accent;
        }

        public static void PublishToApplication(ResourceDictionary res)
        {
            var appResources = System.Windows.Application.Current?.Resources;
            if (appResources == null) return;

            foreach (var key in ApplicationKeys)
            {
                appResources[key] = res[key];
            }
        }

        public static void Set(ResourceDictionary res, string key, MediaColor color) => res[key] = new SolidColorBrush(color);

        public static MediaColor Rgb(byte r, byte g, byte b) => MediaColor.FromRgb(r, g, b);

        public static MediaColor WithAlpha(MediaColor c, byte alpha) => MediaColor.FromArgb(alpha, c.R, c.G, c.B);

        public static MediaColor ParseColor(string? hex, MediaColor fallback)
            => TryParseHex(hex, out byte r, out byte g, out byte b) ? MediaColor.FromRgb(r, g, b) : fallback;

        /// <summary>Parses "#RRGGBB" or "RRGGBB".</summary>
        public static bool TryParseHex(string? hex, out byte r, out byte g, out byte b)
        {
            r = g = b = 0;
            if (string.IsNullOrWhiteSpace(hex)) return false;

            string clean = hex.Trim().TrimStart('#');
            return clean.Length == 6 &&
                   byte.TryParse(clean.AsSpan(0, 2), NumberStyles.HexNumber, null, out r) &&
                   byte.TryParse(clean.AsSpan(2, 2), NumberStyles.HexNumber, null, out g) &&
                   byte.TryParse(clean.AsSpan(4, 2), NumberStyles.HexNumber, null, out b);
        }

        /// <summary>HSV → RGB (h: 0-360, s/v: 0-1).</summary>
        public static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1.0 - Math.Abs((h / 60.0) % 2.0 - 1.0));
            double m = v - c;

            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return ((byte)Math.Round((r + m) * 255.0),
                    (byte)Math.Round((g + m) * 255.0),
                    (byte)Math.Round((b + m) * 255.0));
        }

        public static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            v = max;
            s = max == 0.0 ? 0.0 : delta / max;

            if (delta == 0.0) h = 0.0;
            else if (max == rd) h = 60.0 * (((gd - bd) / delta) % 6.0);
            else if (max == gd) h = 60.0 * (((bd - rd) / delta) + 2.0);
            else h = 60.0 * (((rd - gd) / delta) + 4.0);

            if (h < 0.0) h += 360.0;
        }
    }
}
