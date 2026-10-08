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

        public static Icon CreateAppIcon()
        {
            try
            {
                // 1. Try local Assets directory beside the executable
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string localIco = Path.Combine(baseDir, "Assets", "app.ico");
                if (File.Exists(localIco))
                {
                    return new Icon(localIco);
                }

                // 2. Try embedded resource stream EHelper.Assets.app.ico
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("EHelper.Assets.app.ico"))
                {
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }

                // 3. Try WPF pack URI
                try
                {
                    var uri = new Uri("pack://application:,,,/E-Helper;component/Assets/app.ico", UriKind.Absolute);
                    var sri = System.Windows.Application.GetResourceStream(uri);
                    if (sri?.Stream != null)
                    {
                        using (sri.Stream)
                        {
                            return new Icon(sri.Stream);
                        }
                    }
                }
                catch { }

                // 4. Try tray.ico embedded stream
                using (var stream = asm.GetManifestResourceStream("EHelper.Assets.tray.ico"))
                {
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }

                // 5. Try logo.png / tray.png beside executable
                string localPng = Path.Combine(baseDir, "Assets", "tray.png");
                if (File.Exists(localPng))
                {
                    using var bmp = new Bitmap(localPng);
                    IntPtr h = bmp.GetHicon();
                    try
                    {
                        using var tmp = Icon.FromHandle(h);
                        return (Icon)tmp.Clone();
                    }
                    finally { DestroyIcon(h); }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IconHelper] CreateAppIcon failed: {ex.Message}");
            }

            return GenerateFallbackIcon();
        }

        public static Icon CreateTrayIcon()
        {
            try
            {
                var targetSize = SystemInformation.SmallIconSize;
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // 1. Try local Assets/tray.ico beside executable
                string localIco = Path.Combine(baseDir, "Assets", "tray.ico");
                if (File.Exists(localIco))
                {
                    return new Icon(localIco, targetSize);
                }

                // 2. Try embedded resource stream EHelper.Assets.tray.ico
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("EHelper.Assets.tray.ico"))
                {
                    if (stream != null)
                    {
                        return new Icon(stream, targetSize);
                    }
                }

                // 3. Try WPF Pack URI
                try
                {
                    var uri = new Uri("pack://application:,,,/E-Helper;component/Assets/tray.ico", UriKind.Absolute);
                    var sri = System.Windows.Application.GetResourceStream(uri);
                    if (sri?.Stream != null)
                    {
                        using (sri.Stream)
                        {
                            return new Icon(sri.Stream, targetSize);
                        }
                    }
                }
                catch { }

                // 4. Try embedded EHelper.Assets.tray.png
                using (var stream = asm.GetManifestResourceStream("EHelper.Assets.tray.png"))
                {
                    if (stream != null)
                    {
                        using var originalBmp = new Bitmap(stream);
                        int sz = Math.Max(16, targetSize.Width);
                        using var resized = new Bitmap(sz, sz);
                        using (var g = Graphics.FromImage(resized))
                        {
                            g.Clear(Color.Transparent);
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.DrawImage(originalBmp, 0, 0, sz, sz);
                        }

                        IntPtr h = resized.GetHicon();
                        try
                        {
                            using var tmp = Icon.FromHandle(h);
                            return (Icon)tmp.Clone();
                        }
                        finally { DestroyIcon(h); }
                    }
                }

                // 5. Try local Assets/tray.png
                string localPng = Path.Combine(baseDir, "Assets", "tray.png");
                if (File.Exists(localPng))
                {
                    using var originalBmp = new Bitmap(localPng);
                    int sz = Math.Max(16, targetSize.Width);
                    using var resized = new Bitmap(sz, sz);
                    using (var g = Graphics.FromImage(resized))
                    {
                        g.Clear(Color.Transparent);
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(originalBmp, 0, 0, sz, sz);
                    }

                    IntPtr h = resized.GetHicon();
                    try
                    {
                        using var tmp = Icon.FromHandle(h);
                        return (Icon)tmp.Clone();
                    }
                    finally { DestroyIcon(h); }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IconHelper] CreateTrayIcon failed: {ex.Message}");
            }

            return CreateAppIcon();
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
    }
}
