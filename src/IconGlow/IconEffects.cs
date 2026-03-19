using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BeautyCons.IconGlow
{
    public class IconEffects
    {
        // Shine sweep (standalone)
        private Canvas _shineCanvas;
        private Rectangle _shineBar;
        private double _shineSweepPos;

        // Shimmer: tilt + synchronized shine sweep
        private Canvas _shimmerCanvas;
        private Rectangle _shimmerBar;
        private Grid _shimmerTargetGrid; // the wrapper we apply tilt to
        private SkewTransform _tiltSkewX;
        private ScaleTransform _tiltScaleY;
        private TransformGroup _tiltTransformGroup;
        private double _shimmerPhase; // 0..2 ping-pong (0→1 forward, 1→2 return)
        private Color _shimColor1, _shimColor2;
        private double _iconW, _iconH;

        // Tilt parameters
        private const double MaxSkewDeg = 3.5;   // subtle skew angle
        private const double MaxScaleY = 0.015;   // slight vertical compress at peak tilt

        public void ApplyShineSweep(Grid wrapperGrid, Image icon)
        {
            RemoveShineSweep(wrapperGrid);
            if (icon == null) return;

            double iconW = icon.ActualWidth > 0 ? icon.ActualWidth : 64;
            double iconH = icon.ActualHeight > 0 ? icon.ActualHeight : 64;

            _shineCanvas = new Canvas
            {
                Width = iconW,
                Height = iconH,
                IsHitTestVisible = false,
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            double barWidth = iconW * 0.25;
            _shineBar = new Rectangle
            {
                Width = barWidth,
                Height = iconH * 2,
                Fill = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(0, 255, 255, 255), 0),
                        new GradientStop(Color.FromArgb(50, 255, 255, 255), 0.3),
                        new GradientStop(Color.FromArgb(100, 255, 255, 255), 0.5),
                        new GradientStop(Color.FromArgb(50, 255, 255, 255), 0.7),
                        new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                    },
                    new Point(0, 0.5),
                    new Point(1, 0.5)),
                RenderTransform = new RotateTransform(20, barWidth / 2, iconH)
            };

            Canvas.SetTop(_shineBar, -iconH * 0.5);
            Canvas.SetLeft(_shineBar, -barWidth);
            _shineSweepPos = -barWidth;

            _shineCanvas.Children.Add(_shineBar);
            wrapperGrid.Children.Add(_shineCanvas);
        }

        public void UpdateShineSweep(double speed)
        {
            if (_shineBar == null || _shineCanvas == null) return;

            double iconW = _shineCanvas.Width;
            double barW = _shineBar.Width;
            double totalTravel = iconW + barW * 2;

            _shineSweepPos += totalTravel / (speed * 60.0);
            if (_shineSweepPos > iconW + barW)
                _shineSweepPos = -barW;

            Canvas.SetLeft(_shineBar, _shineSweepPos);
        }

        public void RemoveShineSweep(Grid wrapperGrid)
        {
            if (_shineCanvas != null && wrapperGrid != null)
            {
                if (wrapperGrid.Children.Contains(_shineCanvas))
                    wrapperGrid.Children.Remove(_shineCanvas);
                _shineCanvas = null;
                _shineBar = null;
            }
        }

        /// <summary>
        /// Shimmer: combines a subtle skew-based tilt with a synchronized
        /// color-tinted shine sweep. As the icon "tilts" one way, the light
        /// streak sweeps across in sync — like tilting a glossy card under light.
        /// </summary>
        public void ApplyShimmer(Grid wrapperGrid, Image icon, double opacity, Color color1, Color color2)
        {
            RemoveShimmer(wrapperGrid);
            if (icon == null) return;

            _shimColor1 = color1;
            _shimColor2 = color2;
            _shimmerTargetGrid = wrapperGrid;
            _iconW = icon.ActualWidth > 0 ? icon.ActualWidth : 64;
            _iconH = icon.ActualHeight > 0 ? icon.ActualHeight : 64;

            // Set up tilt transforms on the wrapper grid
            _tiltSkewX = new SkewTransform(0, 0, _iconW / 2, _iconH / 2);
            _tiltScaleY = new ScaleTransform(1, 1, _iconW / 2, _iconH / 2);
            _tiltTransformGroup = new TransformGroup();
            _tiltTransformGroup.Children.Add(_tiltSkewX);
            _tiltTransformGroup.Children.Add(_tiltScaleY);
            wrapperGrid.RenderTransform = _tiltTransformGroup;

            // Shine sweep canvas — clipped to icon bounds
            _shimmerCanvas = new Canvas
            {
                Width = _iconW,
                Height = _iconH,
                IsHitTestVisible = false,
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = opacity
            };

            // Wide diffused shine bar — soft wash, not a sharp streak
            double barWidth = _iconW * 1.0;
            _shimmerBar = new Rectangle
            {
                Width = barWidth,
                Height = _iconH * 2.5,
                IsHitTestVisible = false,
                RenderTransform = new RotateTransform(18, barWidth / 2, _iconH * 1.25)
            };

            Canvas.SetTop(_shimmerBar, -_iconH * 0.75);
            Canvas.SetLeft(_shimmerBar, -barWidth);

            _shimmerCanvas.Children.Add(_shimmerBar);
            wrapperGrid.Children.Add(_shimmerCanvas);

            _shimmerPhase = 0;
            UpdateShimmerFrame(0);
        }

        public void UpdateShimmer(double speed)
        {
            if (_shimmerBar == null || _shimmerCanvas == null) return;

            // Ping-pong: 0→1 forward, 1→2 return
            double step = 1.0 / (speed * 60.0);
            _shimmerPhase += step;
            if (_shimmerPhase > 2.0)
                _shimmerPhase -= 2.0;

            double t = _shimmerPhase <= 1.0 ? _shimmerPhase : 2.0 - _shimmerPhase;
            UpdateShimmerFrame(t);
        }

        /// <param name="t">0..1 normalized sweep position (0=left, 1=right)</param>
        private void UpdateShimmerFrame(double t)
        {
            if (_shimmerBar == null) return;

            // --- TILT ---
            // Sine-smoothed tilt: peaks at edges, neutral at center
            // t=0: tilted left, t=0.5: flat, t=1: tilted right
            double tiltAmount = Math.Sin((t - 0.5) * Math.PI); // -1 to +1
            if (_tiltSkewX != null)
                _tiltSkewX.AngleX = tiltAmount * MaxSkewDeg;
            if (_tiltScaleY != null)
                _tiltScaleY.ScaleY = 1.0 - Math.Abs(tiltAmount) * MaxScaleY;

            // --- SHINE POSITION ---
            // Sweep synchronized with tilt direction
            double barW = _shimmerBar.Width;
            double travelStart = -barW;
            double travelEnd = _iconW + barW;
            double barPos = travelStart + (travelEnd - travelStart) * t;
            Canvas.SetLeft(_shimmerBar, barPos);

            // --- SHINE OPACITY ---
            // Bell curve: brightest when crossing center of icon
            double centerDist = Math.Abs(t - 0.5) * 2.0; // 0 at center, 1 at edges
            double shineOpacity = 1.0 - centerDist * centerDist; // quadratic falloff
            _shimmerBar.Opacity = shineOpacity;

            // --- SHINE GRADIENT ---
            // Blend between icon colors with white specular center
            // Subtle hue shift based on position for iridescence
            double hueShift = (t - 0.5) * 30.0;
            Color c1 = Brighten(ShiftHue(_shimColor1, hueShift), 0.55);
            Color c2 = Brighten(ShiftHue(_shimColor2, -hueShift), 0.55);

            _shimmerBar.Fill = new LinearGradientBrush(
                new GradientStopCollection
                {
                    // Very gradual ramp in
                    new GradientStop(Color.FromArgb(0,   c1.R, c1.G, c1.B), 0.00),
                    new GradientStop(Color.FromArgb(25,  c1.R, c1.G, c1.B), 0.15),
                    new GradientStop(Color.FromArgb(60,  c1.R, c1.G, c1.B), 0.30),
                    // Soft specular glow — diffused, not a hard line
                    new GradientStop(Color.FromArgb(90,  255, 255, 255), 0.40),
                    new GradientStop(Color.FromArgb(120, 255, 255, 255), 0.50),
                    new GradientStop(Color.FromArgb(90,  255, 255, 255), 0.60),
                    // Gradual ramp out
                    new GradientStop(Color.FromArgb(60,  c2.R, c2.G, c2.B), 0.70),
                    new GradientStop(Color.FromArgb(25,  c2.R, c2.G, c2.B), 0.85),
                    new GradientStop(Color.FromArgb(0,   c2.R, c2.G, c2.B), 1.00),
                },
                new Point(0, 0),
                new Point(1, 0));
        }

        public void RemoveShimmer(Grid wrapperGrid)
        {
            // Remove shine canvas
            if (_shimmerCanvas != null && wrapperGrid != null)
            {
                if (wrapperGrid.Children.Contains(_shimmerCanvas))
                    wrapperGrid.Children.Remove(_shimmerCanvas);
                _shimmerCanvas = null;
                _shimmerBar = null;
            }

            // Reset tilt transform
            if (_shimmerTargetGrid != null)
            {
                _shimmerTargetGrid.RenderTransform = null;
                _shimmerTargetGrid = null;
                _tiltSkewX = null;
                _tiltScaleY = null;
                _tiltTransformGroup = null;
            }
        }

        private static Color Brighten(Color c, double amount)
        {
            return Color.FromRgb(
                (byte)Math.Min(255, c.R + (255 - c.R) * amount),
                (byte)Math.Min(255, c.G + (255 - c.G) * amount),
                (byte)Math.Min(255, c.B + (255 - c.B) * amount));
        }

        private static Color ShiftHue(Color color, double degrees)
        {
            double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;
            double h = 0, s = max > 0 ? delta / max : 0, v = max;

            if (delta > 0)
            {
                if (max == r) h = 60 * (((g - b) / delta) % 6);
                else if (max == g) h = 60 * (((b - r) / delta) + 2);
                else h = 60 * (((r - g) / delta) + 4);
            }
            if (h < 0) h += 360;

            h = (h + degrees) % 360;
            if (h < 0) h += 360;

            double c2 = v * s;
            double x = c2 * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c2;
            double r1, g1, b1;
            if (h < 60)       { r1 = c2; g1 = x;  b1 = 0; }
            else if (h < 120) { r1 = x;  g1 = c2; b1 = 0; }
            else if (h < 180) { r1 = 0;  g1 = c2; b1 = x; }
            else if (h < 240) { r1 = 0;  g1 = x;  b1 = c2; }
            else if (h < 300) { r1 = x;  g1 = 0;  b1 = c2; }
            else              { r1 = c2; g1 = 0;  b1 = x; }

            return Color.FromRgb(
                (byte)Math.Min(255, (r1 + m) * 255),
                (byte)Math.Min(255, (g1 + m) * 255),
                (byte)Math.Min(255, (b1 + m) * 255));
        }

        public void RemoveAll(Grid wrapperGrid)
        {
            RemoveShineSweep(wrapperGrid);
            RemoveShimmer(wrapperGrid);
        }
    }
}
