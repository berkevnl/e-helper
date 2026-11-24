using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EHelper.UI
{
    public static class IconHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private static readonly Assembly Asm = Assembly.GetExecutingAssembly();
        private static string LocalAsset(string fileName) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);

        public static Icon CreateTrayIcon()
        {
            try
            {
                var targetSize = SystemInformation.SmallIconSize;
                int sz = Math.Max(16, targetSize.Width);

                // 1. Local Assets/tray.ico beside executable
                string localIco = LocalAsset("tray.ico");
                if (File.Exists(localIco))
                {
                    return new Icon(localIco, targetSize);
                }

                // 2. Embedded tray.ico
                using (var stream = Asm.GetManifestResourceStream("EHelper.Assets.tray.ico"))
                {
                    if (stream != null) return new Icon(stream, targetSize);
                }

                // 3. WPF pack URI
                var packIcon = TryLoadPackIcon("tray.ico", targetSize);
                if (packIcon != null) return packIcon;

                // 4. Embedded tray.png
                using (var stream = Asm.GetManifestResourceStream("EHelper.Assets.tray.png"))
                {
                    if (stream != null)
                    {
                        using var bmp = new Bitmap(stream);
                        return ResizeToIcon(bmp, sz);
                    }
                }

                // 5. Local Assets/tray.png
                string localPng = LocalAsset("tray.png");
                if (File.Exists(localPng))
                {
                    using var bmp = new Bitmap(localPng);
                    return ResizeToIcon(bmp, sz);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IconHelper] CreateTrayIcon failed: {ex.Message}");
            }

            return CreateAppIcon();
        }

        private static Icon CreateAppIcon()
        {
            try
            {
                // 1. Local Assets/app.ico beside executable
                string localIco = LocalAsset("app.ico");
                if (File.Exists(localIco))
                {
                    return new Icon(localIco);
                }

                // 2. Embedded app.ico
                using (var stream = Asm.GetManifestResourceStream("EHelper.Assets.app.ico"))
                {
                    if (stream != null) return new Icon(stream);
                }

                // 3. WPF pack URI
                var packIcon = TryLoadPackIcon("app.ico", null);
                if (packIcon != null) return packIcon;

                // 4. Embedded tray.ico
                using (var stream = Asm.GetManifestResourceStream("EHelper.Assets.tray.ico"))
                {
                    if (stream != null) return new Icon(stream);
                }

                // 5. Local Assets/tray.png
                string localPng = LocalAsset("tray.png");
                if (File.Exists(localPng))
                {
                    using var bmp = new Bitmap(localPng);
                    return BitmapToIcon(bmp);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IconHelper] CreateAppIcon failed: {ex.Message}");
            }

            return GenerateFallbackIcon();
        }

        private static Icon? TryLoadPackIcon(string fileName, Size? size)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/E-Helper;component/Assets/{fileName}", UriKind.Absolute);
                var sri = System.Windows.Application.GetResourceStream(uri);
                if (sri?.Stream != null)
                {
                    using (sri.Stream)
                    {
                        return size.HasValue ? new Icon(sri.Stream, size.Value) : new Icon(sri.Stream);
                    }
                }
            }
            catch { }
            return null;
        }

        private static Icon ResizeToIcon(Bitmap source, int size)
        {
            using var resized = new Bitmap(size, size);
            using (var g = Graphics.FromImage(resized))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(source, 0, 0, size, size);
            }
            return BitmapToIcon(resized);
        }

        /// <summary>Converts a bitmap to a managed Icon and releases the native HICON.</summary>
        private static Icon BitmapToIcon(Bitmap bmp)
        {
            IntPtr hIcon = bmp.GetHicon();
            try
            {
                using var temp = Icon.FromHandle(hIcon);
                return (Icon)temp.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        private static Icon GenerateFallbackIcon()
        {
            const int size = 64;
            using var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                // Sleek dark squircle with neon green border & badge
                using var bgBrush = new SolidBrush(Color.FromArgb(255, 26, 29, 34));
                using var greenPen = new Pen(Color.FromArgb(255, 34, 197, 94), 2.5f);
                g.FillEllipse(bgBrush, 2, 2, size - 5, size - 5);
                g.DrawEllipse(greenPen, 2, 2, size - 5, size - 5);

                // Neon green bold 'E'
                using var textBrush = new SolidBrush(Color.FromArgb(255, 34, 197, 94));
                using var font = new Font("Segoe UI", 24, FontStyle.Bold, GraphicsUnit.Pixel);
                using var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                g.DrawString("E", font, textBrush, new RectangleF(0, -1, size, size), format);
            }

            return BitmapToIcon(bmp);
        }
    }
}
