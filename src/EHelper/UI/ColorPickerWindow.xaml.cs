using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Color = System.Windows.Media.Color;
using Button = System.Windows.Controls.Button;

namespace EHelper.UI
{
    public partial class ColorPickerWindow : Window
    {
        public byte SelectedRed { get; private set; }
        public byte SelectedGreen { get; private set; }
        public byte SelectedBlue { get; private set; }

        public event Action<byte, byte, byte>? ColorPreviewChanged;

        private double _hue = 0.0;          // 0.0 - 360.0
        private double _saturation = 1.0;   // 0.0 - 1.0
        private double _value = 1.0;        // 0.0 - 1.0

        private bool _isUpdating = true;
        private bool _isDraggingSv;
        private bool _isDraggingHue;

        private double _lastRenderedHue = -1;
        private byte _lastReportedR = 255, _lastReportedG = 255, _lastReportedB = 255;
        private readonly SolidColorBrush _newColorBrush = new();

        public ColorPickerWindow(byte initialR, byte initialG, byte initialB)
        {
            _isUpdating = true;
            InitializeComponent();

            SelectedRed = initialR;
            SelectedGreen = initialG;
            SelectedBlue = initialB;

            RgbToHsv(initialR, initialG, initialB, out _hue, out _saturation, out _value);

            if (TxtHexInput != null)
            {
                TxtHexInput.Text = $"#{initialR:X2}{initialG:X2}{initialB:X2}";
                TxtHexInput.TextChanged += TxtHexInput_TextChanged;
            }

            if (TxtDialogTitle != null)
            {
                TxtDialogTitle.Text = Services.LocalizationManager.ColorDialogTitle;
            }
            if (BtnApply != null)
            {
                BtnApply.Content = Services.LocalizationManager.ColorOkButton;
            }
            if (BtnCancel != null)
            {
                BtnCancel.Content = Services.LocalizationManager.ColorCancelButton;
            }

            Loaded += (s, e) =>
            {
                _isUpdating = false;
                UpdateFromHsv(updateInputs: true, updateReticle: true, updateHueThumb: true);
            };
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        #region SV Canvas Interaction

        private void SvBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDraggingSv = true;
                Mouse.Capture(sender as IInputElement);
                UpdateSvFromMouse(e.GetPosition(GridSvBox));
            }
        }

        private void SvBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingSv && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSvFromMouse(e.GetPosition(GridSvBox));
            }
        }

        private void SvBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingSv)
            {
                _isDraggingSv = false;
                Mouse.Capture(null);
            }
        }

        private void UpdateSvFromMouse(Point p)
        {
            if (GridSvBox == null || GridSvBox.ActualWidth <= 0 || GridSvBox.ActualHeight <= 0) return;

            double s = Math.Clamp(p.X / GridSvBox.ActualWidth, 0.0, 1.0);
            double v = Math.Clamp(1.0 - (p.Y / GridSvBox.ActualHeight), 0.0, 1.0);

            _saturation = s;
            _value = v;

            UpdateFromHsv(updateInputs: true, updateReticle: true, updateHueThumb: false);
        }

        #endregion

        #region Vertical Hue Bar Interaction

        private void HueBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDraggingHue = true;
                Mouse.Capture(sender as IInputElement);
                UpdateHueFromMouse(e.GetPosition(GridHueBar));
            }
        }

        private void HueBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingHue && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateHueFromMouse(e.GetPosition(GridHueBar));
            }
        }

        private void HueBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingHue)
            {
                _isDraggingHue = false;
                Mouse.Capture(null);
            }
        }

        private void UpdateHueFromMouse(Point p)
        {
            if (GridHueBar == null || GridHueBar.ActualHeight <= 0) return;

            // Vertical gradient from top (0.0 = 0° Red) to bottom (1.0 = 360° Red)
            double ratio = Math.Clamp(p.Y / GridHueBar.ActualHeight, 0.0, 1.0);
            _hue = ratio * 360.0;
            if (_hue >= 360.0) _hue = 0.0;

            UpdateFromHsv(updateInputs: true, updateReticle: false, updateHueThumb: true);
        }

        #endregion

        #region Text Inputs & Swatches

        private void TxtHexInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating || TxtHexInput == null) return;

            if (ThemeHelper.TryParseHex(TxtHexInput.Text, out byte r, out byte g, out byte b))
            {
                RgbToHsv(r, g, b, out _hue, out _saturation, out _value);
                UpdateFromHsv(updateInputs: false, updateReticle: true, updateHueThumb: true);
            }
        }

        private void Swatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex && ThemeHelper.TryParseHex(hex, out byte r, out byte g, out byte b))
            {
                RgbToHsv(r, g, b, out _hue, out _saturation, out _value);
                UpdateFromHsv(updateInputs: true, updateReticle: true, updateHueThumb: true);
            }
        }

        #endregion

        #region HSV Calculations

        private void UpdateFromHsv(bool updateInputs, bool updateReticle, bool updateHueThumb)
        {
            try
            {
                var (r, g, b) = HsvToRgb(_hue, _saturation, _value);
                SelectedRed = r;
                SelectedGreen = g;
                SelectedBlue = b;

                // 1. Update pure Hue background of 2D SV Box
                if (StopHueColor != null && Math.Abs(_hue - _lastRenderedHue) > 0.05)
                {
                    _lastRenderedHue = _hue;
                    var (pureR, pureG, pureB) = HsvToRgb(_hue, 1.0, 1.0);
                    StopHueColor.Color = Color.FromRgb(pureR, pureG, pureB);
                }

                // 2. Update Color Preview Box
                if (BrdNewColor != null)
                {
                    _newColorBrush.Color = Color.FromRgb(r, g, b);
                    if (!ReferenceEquals(BrdNewColor.Background, _newColorBrush))
                    {
                        BrdNewColor.Background = _newColorBrush;
                    }
                }

                // 3. Update Text Inputs and Labels
                if (updateInputs)
                {
                    _isUpdating = true;
                    if (TxtHexInput != null) TxtHexInput.Text = $"#{r:X2}{g:X2}{b:X2}";
                    _isUpdating = false;
                }

                if (TxtRVal != null) TxtRVal.Text = r.ToString();
                if (TxtGVal != null) TxtGVal.Text = g.ToString();
                if (TxtBVal != null) TxtBVal.Text = b.ToString();

                // 4. Update Reticle Indicator Position on 2D SV Box
                if (updateReticle && GridSvBox != null && Reticle != null && GridSvBox.ActualWidth > 0 && GridSvBox.ActualHeight > 0)
                {
                    double x = _saturation * GridSvBox.ActualWidth - (Reticle.Width / 2.0);
                    double y = (1.0 - _value) * GridSvBox.ActualHeight - (Reticle.Height / 2.0);
                    Canvas.SetLeft(Reticle, Math.Clamp(x, -Reticle.Width / 2.0, GridSvBox.ActualWidth - Reticle.Width / 2.0));
                    Canvas.SetTop(Reticle, Math.Clamp(y, -Reticle.Height / 2.0, GridSvBox.ActualHeight - Reticle.Height / 2.0));
                }

                // 5. Update Hue Vertical Indicator Position
                if (updateHueThumb && GridHueBar != null && HueThumb != null && GridHueBar.ActualHeight > 0)
                {
                    double thumbY = (_hue / 360.0) * (GridHueBar.ActualHeight - HueThumb.Height);
                    Canvas.SetTop(HueThumb, Math.Clamp(thumbY, 0, GridHueBar.ActualHeight - HueThumb.Height));
                }

                // 6. Broadcast Real-time Live Preview
                if (r != _lastReportedR || g != _lastReportedG || b != _lastReportedB)
                {
                    _lastReportedR = r;
                    _lastReportedG = g;
                    _lastReportedB = b;
                    ColorPreviewChanged?.Invoke(r, g, b);
                }
            }
            catch
            {
                // Guard against transient layout interrupts
            }
        }

        private static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v) => ThemeHelper.HsvToRgb(h, s, v);

        private static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
            => ThemeHelper.RgbToHsv(r, g, b, out h, out s, out v);

        #endregion
    }
}
