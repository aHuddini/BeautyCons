using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BeautyCons.IconGlow
{
    public class SparkleOverlay
    {
        private readonly Canvas _canvas;
        private readonly Random _rng = new Random();
        private Sparkle[] _sparkles;
        private Color _color1, _color2;
        private double _iconWidth, _iconHeight, _extend;

        private struct Sparkle
        {
            public Ellipse Element;
            public double Angle;      // position angle around icon
            public double Radius;     // distance from center
            public double Phase;      // lifecycle 0→1
            public double PhaseSpeed; // how fast it cycles
            public double Size;
        }

        public Canvas Canvas => _canvas;

        public SparkleOverlay()
        {
            _canvas = new Canvas
            {
                IsHitTestVisible = false,
                ClipToBounds = false
            };
        }

        public void Configure(double iconWidth, double iconHeight, double extend,
            Color color1, Color color2, int count)
        {
            _iconWidth = iconWidth;
            _iconHeight = iconHeight;
            _extend = extend;
            _color1 = color1;
            _color2 = color2;

            _canvas.Children.Clear();
            _sparkles = new Sparkle[count];

            for (int i = 0; i < count; i++)
            {
                var ellipse = new Ellipse
                {
                    IsHitTestVisible = false,
                    Opacity = 0
                };
                _canvas.Children.Add(ellipse);

                _sparkles[i] = new Sparkle
                {
                    Element = ellipse,
                    Angle = _rng.NextDouble() * 360,
                    Radius = 0.5 + _rng.NextDouble() * 0.5, // 50-100% of extend
                    Phase = _rng.NextDouble(),
                    PhaseSpeed = 0.3 + _rng.NextDouble() * 0.7,
                    Size = 2 + _rng.NextDouble() * 3
                };

                ellipse.Width = _sparkles[i].Size;
                ellipse.Height = _sparkles[i].Size;

                // Alternate colors
                var color = i % 2 == 0 ? _color1 : _color2;
                ellipse.Fill = new SolidColorBrush(color);
            }
        }

        public void Update(double speed)
        {
            if (_sparkles == null) return;

            double cx = _iconWidth / 2;
            double cy = _iconHeight / 2;
            double baseRadius = Math.Max(_iconWidth, _iconHeight) / 2;

            for (int i = 0; i < _sparkles.Length; i++)
            {
                ref var s = ref _sparkles[i];

                // Advance phase
                s.Phase += s.PhaseSpeed * speed * 0.016; // ~per frame at 60fps
                if (s.Phase >= 1.0)
                {
                    // Respawn at new random position
                    s.Phase = 0;
                    s.Angle = _rng.NextDouble() * 360;
                    s.Radius = 0.5 + _rng.NextDouble() * 0.5;
                    s.PhaseSpeed = 0.3 + _rng.NextDouble() * 0.7;
                }

                // Fade in then out: peak at phase 0.3
                double opacity;
                if (s.Phase < 0.3)
                    opacity = s.Phase / 0.3;
                else
                    opacity = 1.0 - (s.Phase - 0.3) / 0.7;
                opacity = Math.Max(0, Math.Min(1, opacity));

                // Position on elliptical path around icon
                double rad = s.Angle * Math.PI / 180;
                double dist = baseRadius + _extend * s.Radius;
                double x = cx + Math.Cos(rad) * dist * (_iconWidth / _iconHeight) - s.Size / 2;
                double y = cy + Math.Sin(rad) * dist - s.Size / 2;

                // Slowly drift angle
                s.Angle += s.PhaseSpeed * speed * 0.5;

                s.Element.Opacity = opacity * 0.8;
                Canvas.SetLeft(s.Element, x);
                Canvas.SetTop(s.Element, y);
            }
        }

        public void Clear()
        {
            _canvas.Children.Clear();
            _sparkles = null;
        }
    }
}
