using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Playnite.SDK;
using SkiaSharp;

namespace BeautyCons.IconGlow
{
    public class IconEffects
    {
        // Effect shape (square = directional, circular = radial)
        private EffectShape _effectShape;

        // Shine sweep (standalone effect)
        private Canvas _shineCanvas;
        private Rectangle _shineBar;
        private Ellipse _shineEllipse;
        private double _shineSweepPos;
        private double _shineSweepAngle;
        private bool _shinePaused;
        private double _shinePauseUntil;

        // Shimmer + tilt share one cycle clock
        private Image _shimmerOverlay;
        private Grid _shimmerGrid;
        private SkewTransform _tiltSkew;
        private ScaleTransform _tiltScale;
        private bool _tiltEnabled;
        private Color _shimColor1, _shimColor2;
        private double _iconW, _iconH;
        private ShineStyle _shineStyle;
        private double _shimmerOpacity;

        // Unified cycle/pause state
        private double _currentPhase; // 0.0–1.0, stored for radial orbit angle
        private double _orbitOffset;  // accumulated orbit angle offset for seamless radial continuity
        private double _cycleStart;
        private double _pauseUntil;
        private bool _paused;
        private bool _flipDir; // alternates sweep direction each cycle

        // Hover state
        private double _hoverBlend;    // 0.0 = autonomous, 1.0 = hover
        private double _hoverMX;       // normalized mouse X [-1, 1]
        private double _hoverMY;       // normalized mouse Y [-1, 1]
        private double _targetHoverBlend; // lerp target (0 or 1)
        private TranslateTransform _tiltTranslate;
        private bool _hoverEventsAttached;

        // Breathing scale
        private bool _breathingEnabled;
        private DateTime _breathStartTime = DateTime.UtcNow;
        private DateTime _lastTickTime = DateTime.UtcNow;

        // Levitation (standalone continuous float)
        private bool _levitationEnabled;
        private double _levitationSpeed = 4.0;
        private double _levitationAmount = 2.5;

        // 3D rotation (fake perspective turntable)
        private bool _3dRotationEnabled;
        private double _rotationSpeed = 6.0;
        private double _rotationAmount = 3.0;

        // Glint (random bright flash)
        private bool _glintEnabled;
        private double _glintNextTime;
        private double _glintIntervalMin = 4.0;
        private double _glintIntervalMax = 10.0;
        private double _glintPhase = -1; // -1 = inactive, 0..1 = flash progress

        // Shadow drift
        private bool _shadowDriftEnabled;

        // Parallax
        private bool _parallaxEnabled;

        // Cached icon pixels for SkiaSharp compositing
        private byte[] _iconPixels;
        private byte[] _luminanceMap; // per-pixel luminance for content-aware masking
        private int _pixW, _pixH;
        private object _cachedIconSource;

        // Reusable SkiaSharp surfaces and WriteableBitmap
        private SKSurface _skiaSurface;
        private SKSurface _effectSurface;
        private int _surfW, _surfH;
        private WriteableBitmap _writeableBmp;

        private static readonly Random Rng = new Random();
        private static readonly ILogger Logger = LogManager.GetLogger();

        private const double MaxSkewDeg = 2.5;
        private const double MaxScaleY = 0.012;
        private int _logThrottle;

        // ----------------------------------------------------------------
        //  SHINE SWEEP
        // ----------------------------------------------------------------

        public void ApplyShineSweep(Grid grid, Image icon, EffectShape shape)
        {
            RemoveShineSweep(grid);
            if (icon == null) return;

            _effectShape = shape;
            double w = icon.ActualWidth > 0 ? icon.ActualWidth : 64;
            double h = icon.ActualHeight > 0 ? icon.ActualHeight : 64;

            _shineCanvas = new Canvas
            {
                Width = w, Height = h,
                IsHitTestVisible = false, ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (shape == EffectShape.Circular)
            {
                // Radial: smaller orbiting ellipse to stay within circular icon
                double ellipseSize = w * 0.25;
                _shineEllipse = new Ellipse
                {
                    Width = ellipseSize, Height = ellipseSize,
                    Fill = new RadialGradientBrush(
                        new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(100, 255, 255, 255), 0),
                            new GradientStop(Color.FromArgb(60, 255, 255, 255), 0.4),
                            new GradientStop(Color.FromArgb(20, 255, 255, 255), 0.7),
                            new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                        })
                };

                // Start at orbit position angle=0 — tight orbit
                double orbitRadius = w * 0.15;
                double cx = w / 2 + orbitRadius - ellipseSize / 2;
                double cy = h / 2 - ellipseSize / 2;
                Canvas.SetLeft(_shineEllipse, cx);
                Canvas.SetTop(_shineEllipse, cy);
                _shineSweepAngle = 0;

                _shineCanvas.Children.Add(_shineEllipse);
            }
            else
            {
                // Directional: diagonal bar sweep
                double barW = w * 0.25;
                _shineBar = new Rectangle
                {
                    Width = barW, Height = h * 2,
                    Fill = new LinearGradientBrush(
                        new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(0, 255, 255, 255), 0),
                            new GradientStop(Color.FromArgb(50, 255, 255, 255), 0.3),
                            new GradientStop(Color.FromArgb(100, 255, 255, 255), 0.5),
                            new GradientStop(Color.FromArgb(50, 255, 255, 255), 0.7),
                            new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                        },
                        new Point(0, 0.5), new Point(1, 0.5)),
                    RenderTransform = new RotateTransform(20, barW / 2, h)
                };

                Canvas.SetTop(_shineBar, -h * 0.5);
                Canvas.SetLeft(_shineBar, -barW);
                _shineSweepPos = -barW;

                _shineCanvas.Children.Add(_shineBar);
            }

            grid.Children.Add(_shineCanvas);
        }

        public void UpdateShineSweep(double speed, double now, double pauseMin, double pauseMax)
        {
            if (_shineCanvas == null) return;

            // Pause between sweeps/orbits
            if (_shinePaused)
            {
                if (now >= _shinePauseUntil)
                    _shinePaused = false;
                else
                    return;
            }

            if (_shineEllipse != null)
            {
                // Radial: orbit the ellipse — tight radius
                double w = _shineCanvas.Width;
                double h = _shineCanvas.Height;
                double orbitRadius = w * 0.15;
                double ellipseSize = _shineEllipse.Width;

                _shineSweepAngle += (2.0 * Math.PI) / (speed * 60.0);
                if (_shineSweepAngle >= 2.0 * Math.PI)
                {
                    _shineSweepAngle -= 2.0 * Math.PI;
                    if (pauseMax > 0)
                    {
                        _shinePaused = true;
                        _shinePauseUntil = now + pauseMin + Rng.NextDouble() * (pauseMax - pauseMin);
                    }
                }

                double cx = w / 2 + Math.Cos(_shineSweepAngle) * orbitRadius - ellipseSize / 2;
                double cy = h / 2 + Math.Sin(_shineSweepAngle) * orbitRadius - ellipseSize / 2;
                Canvas.SetLeft(_shineEllipse, cx);
                Canvas.SetTop(_shineEllipse, cy);
            }
            else if (_shineBar != null)
            {
                // Directional: linear bar sweep
                double w = _shineCanvas.Width;
                double barW = _shineBar.Width;
                _shineSweepPos += (w + barW * 2) / (speed * 60.0);
                if (_shineSweepPos > w + barW)
                {
                    _shineSweepPos = -barW;
                    if (pauseMax > 0)
                    {
                        _shinePaused = true;
                        _shinePauseUntil = now + pauseMin + Rng.NextDouble() * (pauseMax - pauseMin);
                    }
                }
                Canvas.SetLeft(_shineBar, _shineSweepPos);
            }
        }

        public void RemoveShineSweep(Grid grid)
        {
            if (_shineCanvas != null && grid != null && grid.Children.Contains(_shineCanvas))
                grid.Children.Remove(_shineCanvas);
            _shineCanvas = null;
            _shineBar = null;
            _shineEllipse = null;
            _shinePaused = false;
            _shineSweepAngle = 0;
        }

        // ----------------------------------------------------------------
        //  SHIMMER (includes tilt, luster, shine bar — one unified cycle)
        // ----------------------------------------------------------------

        public bool IsShimmerActive => _shimmerOverlay != null;

        public void ApplyShimmer(Grid grid, Image icon, double opacity,
            Color color1, Color color2, ShineStyle style, bool enableTilt, double now,
            EffectShape shape = EffectShape.Square)
        {
            if (icon == null) return;
            _effectShape = shape;

            // Already running on this grid — just update parameters
            if (_shimmerGrid == grid && _shimmerOverlay != null)
            {
                _shimColor1 = color1;
                _shimColor2 = color2;
                _shineStyle = style;
                _shimmerOpacity = opacity;
                _tiltEnabled = enableTilt;

                // Detect icon source change (game switch on reused grid)
                bool iconChanged = !ReferenceEquals(icon.Source, _cachedIconSource);
                CacheIconPixels(icon);

                if (iconChanged)
                {
                    // Clear the old rendered frame immediately so we don't flash the
                    // previous game's luster/shine for a frame or two
                    _shimmerOverlay.Source = null;
                    _shimmerOverlay.Opacity = 0;

                    // Invalidate SkiaSharp surfaces so they get recreated at new size
                    if (_skiaSurface != null) { _skiaSurface.Dispose(); _skiaSurface = null; }
                    if (_effectSurface != null) { _effectSurface.Dispose(); _effectSurface = null; }
                    _surfW = _surfH = 0;
                    _writeableBmp = null;

                    // Update display dimensions for the new icon
                    _iconW = icon.ActualWidth > 0 ? icon.ActualWidth : 64;
                    _iconH = icon.ActualHeight > 0 ? icon.ActualHeight : 64;

                    Logger.Info("[FX] ApplyShimmer: icon changed on same grid, cleared old frame");
                }
                return;
            }

            Logger.Info($"[FX] ApplyShimmer: NEW setup, grid={grid.GetHashCode()}, now={now:F2}");
            RemoveShimmer(grid);

            _shimColor1 = color1;
            _shimColor2 = color2;
            _shineStyle = style;
            _shimmerGrid = grid;
            _shimmerOpacity = opacity;
            _tiltEnabled = enableTilt;
            _iconW = icon.ActualWidth > 0 ? icon.ActualWidth : 64;
            _iconH = icon.ActualHeight > 0 ? icon.ActualHeight : 64;

            CacheIconPixels(icon);

            _shimmerOverlay = new Image
            {
                Width = _iconW, Height = _iconH,
                Stretch = Stretch.Fill, IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0
            };

            grid.Children.Add(_shimmerOverlay);

            // Set up tilt + hover transforms on the grid
            _tiltTranslate = new TranslateTransform(0, 0);
            _tiltSkew = new SkewTransform(0, 0, _iconW / 2, _iconH / 2);
            _tiltScale = new ScaleTransform(1, 1, _iconW / 2, _iconH / 2);
            var group = new TransformGroup();
            group.Children.Add(_tiltTranslate);
            group.Children.Add(_tiltSkew);
            group.Children.Add(_tiltScale);
            grid.RenderTransform = group;

            _paused = false;
            _cycleStart = now;
            _pauseUntil = 0;
        }

        public void UpdateShimmer(double speed, double now, double pauseMin, double pauseMax)
        {
            if (_shimmerOverlay == null) return;

            // Delta time for frame-rate-independent lerp
            double dtSec = (DateTime.UtcNow - _lastTickTime).TotalSeconds;
            _lastTickTime = DateTime.UtcNow;
            dtSec = Math.Min(dtSec, 0.1); // clamp to avoid jumps after stalls

            // Hover blend lerp (always runs, even during shimmer pauses)
            if (Math.Abs(_hoverBlend - _targetHoverBlend) > 0.001)
            {
                double lerpSpeed = _targetHoverBlend > _hoverBlend ? 5.0 : 3.3;
                _hoverBlend += (_targetHoverBlend - _hoverBlend) * Math.Min(1.0, lerpSpeed * dtSec);
                if (Math.Abs(_hoverBlend - _targetHoverBlend) < 0.001)
                    _hoverBlend = _targetHoverBlend;
            }

            // Pause logic
            if (_paused)
            {
                if (now >= _pauseUntil)
                {
                    _paused = false;
                    _cycleStart = now;
                    Logger.Info($"[FX] Pause ended, new cycle at now={now:F2}");
                }
                else
                {
                    _shimmerOverlay.Opacity = 0;
                    // During pause, autonomous tilt is 0 but hover/breathing still apply
                    ApplyTransforms(0, dtSec);
                    return;
                }
            }

            double elapsed = now - _cycleStart;
            double phase = elapsed / speed; // 0.0 → 1.0 over one cycle
            _currentPhase = phase;

            if (phase >= 1.0)
            {
                // Cycle complete — enter pause
                double pauseDur = pauseMin + Rng.NextDouble() * (pauseMax - pauseMin);
                _paused = true;
                _pauseUntil = now + pauseDur;
                _flipDir = !_flipDir; // alternate direction next cycle

                // For radial mode, accumulate orbit offset so the next cycle
                // continues from the same angular position (avoids a visible orbit jump)
                if (_effectShape == EffectShape.Circular)
                    _orbitOffset += 2.0 * Math.PI;

                _shimmerOverlay.Opacity = 0;
                ApplyTransforms(0, dtSec);
                Logger.Info($"[FX] Cycle done at phase={phase:F3}, pausing {pauseDur:F1}s, flipDir={_flipDir}");
                return;
            }

            // --- Waveforms (all start and end at 0) ---
            // Shimmer intensity: sin(phase*PI) → 0→1→0
            double t = Math.Sin(phase * Math.PI);
            // Directional sweep: sin(phase*2*PI) → 0→+1→0→-1→0
            // Alternates direction each cycle so tilt doesn't always go the same way
            double dir = Math.Sin(phase * 2.0 * Math.PI) * (_flipDir ? -1.0 : 1.0);

            // Apply tilt, scale, levitation (blended between autonomous and hover)
            ApplyTransforms(dir, dtSec);

            // Diagnostic logging (every ~1 second)
            _logThrottle++;
            if (_logThrottle >= 60)
            {
                _logThrottle = 0;
                Logger.Info($"[FX] phase={phase:F3} t={t:F3} dir={dir:F3} hover={_hoverBlend:F2} tilt={_tiltSkew?.AngleX:F2}");
            }

            // Blend luster direction: autonomous sine wave vs hover mouse position
            double effectiveDir = dir * (1.0 - _hoverBlend) + _hoverMX * _hoverBlend;
            RenderFrame(t, effectiveDir);
        }

        /// <summary>
        /// Applies tilt, scale, and levitation transforms, blending between autonomous
        /// (shimmer-driven) and hover (mouse-driven) modes based on _hoverBlend.
        /// </summary>
        private void ApplyTransforms(double autoDir, double dtSec)
        {
            // Tilt: blend between autonomous and hover
            if (_tiltSkew != null)
            {
                double autoSkew = _tiltEnabled ? autoDir * MaxSkewDeg : 0;
                double hoverSkew = _hoverMX * 6.0; // ±6° positional tilt
                _tiltSkew.AngleX = autoSkew * (1.0 - _hoverBlend) + hoverSkew * _hoverBlend;
            }

            // Scale composition: breathing × shimmer squash × hover
            if (_tiltScale != null)
            {
                // Breathing: slow continuous pulse (period ~4s), only when enabled
                double breathScale = 1.0;
                if (_breathingEnabled)
                {
                    double breathElapsed = (DateTime.UtcNow - _breathStartTime).TotalSeconds;
                    breathScale = 1.0 + Math.Sin(breathElapsed * 2.0 * Math.PI / 4.0) * 0.02;
                }

                // Shimmer squash (existing)
                double shimmerSquashY = _tiltEnabled ? 1.0 - Math.Abs(autoDir) * MaxScaleY : 1.0;

                // Hover scale: lerp to 1.03
                double hoverScale = 1.0 + _hoverBlend * 0.03;

                // Hover vertical squash from mouse Y
                double hoverSquashY = 1.0 - Math.Abs(_hoverMY) * 0.008 * _hoverBlend;

                _tiltScale.ScaleX = breathScale * hoverScale;
                _tiltScale.ScaleY = breathScale * shimmerSquashY * (1.0 - _hoverBlend)
                                  + breathScale * hoverScale * hoverSquashY * _hoverBlend;
            }

            // Levitation: standalone continuous float + hover bounce
            if (_tiltTranslate != null)
            {
                double levY = 0;
                double elapsed = (DateTime.UtcNow - _breathStartTime).TotalSeconds;

                // Standalone levitation: slow gentle sine wave
                if (_levitationEnabled)
                    levY += Math.Sin(elapsed * 2.0 * Math.PI / _levitationSpeed) * _levitationAmount;

                // Hover levitation (faster bounce, only when hovering)
                if (_hoverBlend > 0.01)
                    levY += Math.Sin(elapsed * 1.5 * 2.0 * Math.PI) * 3.0 * _hoverBlend;

                _tiltTranslate.Y = levY;
            }

            // 3D semi-rotation: fake perspective turntable using skew Y + scale X
            if (_tiltSkew != null && _3dRotationEnabled)
            {
                double elapsed = (DateTime.UtcNow - _breathStartTime).TotalSeconds;
                double rotPhase = Math.Sin(elapsed * 2.0 * Math.PI / _rotationSpeed);

                // SkewY gives the horizontal lean (perspective tilt)
                _tiltSkew.AngleY = rotPhase * _rotationAmount;

                // Slight horizontal squash at extremes simulates foreshortening
                if (_tiltScale != null)
                {
                    double squash = 1.0 - Math.Abs(rotPhase) * 0.03;
                    _tiltScale.ScaleX *= squash;
                }
            }

            // Glint: random bright flash overlay
            if (_glintEnabled && _shimmerOverlay != null)
            {
                double now = (DateTime.UtcNow - _breathStartTime).TotalSeconds;
                if (_glintPhase < 0 && now >= _glintNextTime)
                {
                    _glintPhase = 0; // start flash
                }

                if (_glintPhase >= 0)
                {
                    _glintPhase += dtSec * 4.0; // flash completes in ~0.25s
                    if (_glintPhase >= 1.0)
                    {
                        _glintPhase = -1;
                        _glintNextTime = now + _glintIntervalMin
                            + Rng.NextDouble() * (_glintIntervalMax - _glintIntervalMin);
                    }
                }
            }
        }

        /// <summary>
        /// Updates breathing scale and hover transforms when shimmer is disabled.
        /// When shimmer is active, these are handled inside UpdateShimmer/ApplyTransforms.
        /// </summary>
        public void UpdateHoverAndBreathing()
        {
            // Delta time
            double dtSec = (DateTime.UtcNow - _lastTickTime).TotalSeconds;
            _lastTickTime = DateTime.UtcNow;
            dtSec = Math.Min(dtSec, 0.1);

            // Hover blend lerp
            if (Math.Abs(_hoverBlend - _targetHoverBlend) > 0.001)
            {
                double lerpSpeed = _targetHoverBlend > _hoverBlend ? 5.0 : 3.3;
                _hoverBlend += (_targetHoverBlend - _hoverBlend) * Math.Min(1.0, lerpSpeed * dtSec);
                if (Math.Abs(_hoverBlend - _targetHoverBlend) < 0.001)
                    _hoverBlend = _targetHoverBlend;
            }

            // Reuse ApplyTransforms with autoDir=0 (no shimmer cycle)
            ApplyTransforms(0, dtSec);
        }

        public void RemoveShimmer(Grid grid)
        {
            if (_shimmerOverlay != null && grid != null && grid.Children.Contains(_shimmerOverlay))
                grid.Children.Remove(_shimmerOverlay);
            _shimmerOverlay = null;
            _shimmerGrid = null;
            _iconPixels = null;
            _luminanceMap = null;
            _cachedIconSource = null;
            _writeableBmp = null;

            // Reset tilt transforms
            if (grid != null && _tiltSkew != null)
            {
                _tiltSkew.AngleX = 0;
                _tiltScale.ScaleY = 1.0;
                grid.RenderTransform = null;
            }
            _tiltSkew = null;
            _tiltScale = null;
            _tiltTranslate = null;
            _paused = false;
            ResetHoverState();

            if (_skiaSurface != null)
            {
                _skiaSurface.Dispose();
                _skiaSurface = null;
            }
            if (_effectSurface != null)
            {
                _effectSurface.Dispose();
                _effectSurface = null;
            }
            _surfW = _surfH = 0;
        }

        // ----------------------------------------------------------------
        //  SHIMMER RENDERING
        // ----------------------------------------------------------------

        private void CacheIconPixels(Image icon)
        {
            try
            {
                var src = icon.Source as BitmapSource;
                if (src == null) { _iconPixels = null; _luminanceMap = null; _pixW = _pixH = 0; _cachedIconSource = null; return; }

                // Skip re-caching if the icon source hasn't changed (e.g. settings-only update)
                if (ReferenceEquals(src, _cachedIconSource) && _iconPixels != null)
                    return;

                // Scale to display resolution — the icon is rendered at _iconW x _iconH in WPF,
                // so per-pixel effects must operate at that size to be visible.
                // Working at source resolution (e.g. 1024x1024) is wasted work since WPF
                // downscales the result to ~64x64 anyway, averaging away subtle changes.
                int displayW = (int)_iconW;
                int displayH = (int)_iconH;
                if (displayW <= 0 || displayH <= 0)
                {
                    displayW = 64;
                    displayH = 64;
                }

                // Scale the source to display resolution
                BitmapSource scaled;
                if (src.PixelWidth != displayW || src.PixelHeight != displayH)
                {
                    double scaleX = (double)displayW / src.PixelWidth;
                    double scaleY = (double)displayH / src.PixelHeight;
                    scaled = new TransformedBitmap(src, new ScaleTransform(scaleX, scaleY));
                }
                else
                {
                    scaled = src;
                }

                if (scaled.Format != PixelFormats.Bgra32 && scaled.Format != PixelFormats.Pbgra32)
                    scaled = new FormatConvertedBitmap(scaled, PixelFormats.Bgra32, null, 0);

                _pixW = scaled.PixelWidth;
                _pixH = scaled.PixelHeight;
                int pixelCount = _pixW * _pixH;
                _iconPixels = new byte[pixelCount * 4];
                scaled.CopyPixels(_iconPixels, _pixW * 4, 0);

                // Build luminance map for content-aware masking
                // Gentle curve (no harsh cutoffs), then blur for smooth transitions
                _luminanceMap = new byte[pixelCount];
                for (int i = 0; i < pixelCount; i++)
                {
                    int off = i * 4; // BGRA
                    double rawLum = 0.114 * _iconPixels[off] + 0.587 * _iconPixels[off + 1] + 0.299 * _iconPixels[off + 2];
                    _luminanceMap[i] = (byte)rawLum;
                }
                // Box blur the luminance map so mask edges are soft, not pixel-sharp
                BlurLuminanceMap(_pixW, _pixH, radius: 2);

                _cachedIconSource = icon.Source;
                Logger.Info($"[FX] CacheIconPixels: source {src.PixelWidth}x{src.PixelHeight} -> display {_pixW}x{_pixH} with luminance map");
            }
            catch { _iconPixels = null; _luminanceMap = null; _pixW = _pixH = 0; _cachedIconSource = null; }
        }

        private void RenderFrame(double t, double dir)
        {
            if (_shimmerOverlay == null) return;

            // t is a smooth sine (0→1→0), fades cleanly to 0 at cycle edges
            _shimmerOverlay.Opacity = _shimmerOpacity * t;

            // Use display size (not source image size) so per-pixel effects are visible
            // at the resolution that's actually rendered. Source pixels get averaged away
            // when WPF scales a 1024x1024 image down to 64x64 display size.
            int w = (int)_iconW;
            int h = (int)_iconH;
            if (w <= 0 || h <= 0) return;

            try
            {
                var bmp = RenderShineOverlay(w, h, t, dir);
                if (bmp != null) _shimmerOverlay.Source = bmp;
            }
            catch { }
        }

        private BitmapSource RenderShineOverlay(int w, int h, double t, double dir)
        {
            // Recreate surfaces if dimensions changed (e.g. different icon after game switch)
            if (_skiaSurface != null && (_surfW != w || _surfH != h))
            {
                _skiaSurface.Dispose();
                _skiaSurface = null;
                if (_effectSurface != null) { _effectSurface.Dispose(); _effectSurface = null; }
            }
            var info = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
            if (_skiaSurface == null)
            {
                _skiaSurface = SKSurface.Create(info);
                _surfW = w;
                _surfH = h;
            }
            if (_effectSurface == null)
                _effectSurface = SKSurface.Create(info);
            if (_skiaSurface == null || _effectSurface == null) return null;

            var canvas = _skiaSurface.Canvas;
            canvas.Clear(SKColors.Transparent);

            bool hasLumMask = _luminanceMap != null && _luminanceMap.Length == w * h;

            // --- Draw base icon pixels (Square mode only) ---
            if (_effectShape != EffectShape.Circular && _iconPixels != null && _pixW == w && _pixH == h)
            {
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(
                    _iconPixels, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    using (var bmp = new SKBitmap())
                    {
                        bmp.InstallPixels(
                            new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul),
                            handle.AddrOfPinnedObject(), w * 4);
                        canvas.DrawBitmap(bmp, 0, 0);
                    }
                }
                finally { handle.Free(); }
            }

            // =============================================================
            //  LAYER 2: Shine bar / radial shimmer spot
            // =============================================================
            double hueShift;
            if (_effectShape == EffectShape.Circular)
            {
                double orbitAngleForHue = _orbitOffset + _currentPhase * 2.0 * Math.PI;
                hueShift = Math.Sin(orbitAngleForHue) * 15.0;
            }
            else
            {
                hueShift = dir * 15.0;
            }
            SKColor sc1, sc2, scCenter;
            SKBlendMode blend;

            switch (_shineStyle)
            {
                case ShineStyle.Gold:
                    var g1 = ShiftHue(Color.FromRgb(255, 200, 50), hueShift * 0.5);
                    var g2 = ShiftHue(Color.FromRgb(255, 170, 0), -hueShift * 0.5);
                    sc1 = new SKColor(g1.R, g1.G, g1.B, 240);
                    sc2 = new SKColor(g2.R, g2.G, g2.B, 240);
                    scCenter = new SKColor(255, 240, 180, 255);
                    blend = SKBlendMode.Screen;
                    break;
                case ShineStyle.Holographic:
                    double hh = (t * 360.0) % 360.0;
                    var hc1 = HsvToColor(hh, 0.9, 0.95);
                    var hc2 = HsvToColor((hh + 120) % 360, 0.9, 0.95);
                    var hcc = HsvToColor((hh + 60) % 360, 0.7, 1.0);
                    sc1 = new SKColor(hc1.R, hc1.G, hc1.B, 200);
                    sc2 = new SKColor(hc2.R, hc2.G, hc2.B, 200);
                    scCenter = new SKColor(hcc.R, hcc.G, hcc.B, 235);
                    blend = SKBlendMode.SoftLight;
                    break;
                case ShineStyle.Platinum:
                    var p1 = ShiftHue(Color.FromRgb(200, 220, 255), hueShift * 0.3);
                    var p2 = ShiftHue(Color.FromRgb(170, 190, 230), -hueShift * 0.3);
                    sc1 = new SKColor(p1.R, p1.G, p1.B, 210);
                    sc2 = new SKColor(p2.R, p2.G, p2.B, 210);
                    scCenter = new SKColor(240, 248, 255, 250);
                    blend = SKBlendMode.Overlay;
                    break;
                case ShineStyle.Crimson:
                    var cr1 = ShiftHue(Color.FromRgb(255, 60, 40), hueShift * 0.5);
                    var cr2 = ShiftHue(Color.FromRgb(255, 140, 0), -hueShift * 0.5);
                    sc1 = new SKColor(cr1.R, cr1.G, cr1.B, 220);
                    sc2 = new SKColor(cr2.R, cr2.G, cr2.B, 220);
                    scCenter = new SKColor(255, 200, 150, 245);
                    blend = SKBlendMode.Overlay;
                    break;
                case ShineStyle.IconColors:
                    var ic1 = Saturate(Brighten(_shimColor1, 0.3), 0.8);
                    var ic2 = Saturate(Brighten(_shimColor2, 0.3), 0.8);
                    ic1 = ShiftHue(ic1, hueShift);
                    ic2 = ShiftHue(ic2, -hueShift);
                    sc1 = new SKColor(ic1.R, ic1.G, ic1.B, 200);
                    sc2 = new SKColor(ic2.R, ic2.G, ic2.B, 200);
                    scCenter = new SKColor(
                        (byte)((ic1.R + ic2.R) / 2),
                        (byte)((ic1.G + ic2.G) / 2),
                        (byte)((ic1.B + ic2.B) / 2), 240);
                    blend = SKBlendMode.SoftLight;
                    break;
                default: // White
                    var wc1 = Brighten(_shimColor1, 0.55);
                    var wc2 = Brighten(_shimColor2, 0.55);
                    wc1 = ShiftHue(wc1, hueShift);
                    wc2 = ShiftHue(wc2, -hueShift);
                    sc1 = new SKColor(wc1.R, wc1.G, wc1.B, 210);
                    sc2 = new SKColor(wc2.R, wc2.G, wc2.B, 210);
                    scCenter = new SKColor(255, 255, 255, 250);
                    blend = SKBlendMode.Overlay;
                    break;
            }

            if (_effectShape == EffectShape.Circular)
            {
                // --- Radial shimmer: orbiting highlight spot ---
                // Tighter orbit + gradient to stay within circular icon bounds
                double orbitAngle = _orbitOffset + _currentPhase * 2.0 * Math.PI;
                if (_hoverBlend > 0.01)
                {
                    double hoverAngle = Math.Atan2(_hoverMY, _hoverMX);
                    double delta = hoverAngle - orbitAngle;
                    while (delta > Math.PI) delta -= 2.0 * Math.PI;
                    while (delta < -Math.PI) delta += 2.0 * Math.PI;
                    orbitAngle = orbitAngle + delta * _hoverBlend;
                }

                float spotX = (float)(w * (0.5 + Math.Cos(orbitAngle) * 0.18));
                float spotY = (float)(h * (0.5 + Math.Sin(orbitAngle) * 0.18));
                float gradRadius = w * 0.30f;

                var radialColors = new SKColor[]
                {
                    scCenter,
                    sc1.WithAlpha(180),
                    sc1.WithAlpha(80),
                    sc2.WithAlpha(30),
                    SKColors.Transparent,
                };
                var radialPos = new float[] { 0f, 0.20f, 0.45f, 0.70f, 1f };

                // Clip to circular icon shape
                canvas.Save();
                using (var clipPath = new SKPath())
                {
                    float radius = Math.Min(w, h) * 0.48f;
                    clipPath.AddCircle(w / 2f, h / 2f, radius);
                    canvas.ClipPath(clipPath);
                }

                using (var spotPaint = new SKPaint())
                {
                    spotPaint.IsAntialias = true;
                    spotPaint.BlendMode = SKBlendMode.SrcOver;
                    spotPaint.Shader = SKShader.CreateRadialGradient(
                        new SKPoint(spotX, spotY), gradRadius,
                        radialColors, radialPos, SKShaderTileMode.Clamp);
                    canvas.DrawRect(0, 0, w, h, spotPaint);
                }

                canvas.Restore();
            }
            else
            {
                // --- Directional shimmer: diagonal bar sweep ---
                double barAbsTilt = Math.Abs(dir);

                // Width: narrower at edges (just entering/leaving), wider at center of sweep
                double widthFactor = 0.45 + (1.0 - barAbsTilt) * 0.25;
                float barW = (float)(w * widthFactor);

                // Position: sweep across full icon width
                float barX = (float)(w * 0.5 + (-dir) * w * 0.45);

                // Angle: rotates dynamically as bar sweeps
                float angle = (float)(15.0 + dir * 12.0 + (1.0 - barAbsTilt) * 5.0);

                float barLeft = barX - barW / 2;
                float barRight = barX + barW / 2;

                // Gradient softness
                float innerSpread = (float)(0.06 + barAbsTilt * 0.06);
                var shineColors = new SKColor[]
                {
                    SKColors.Transparent,
                    sc1.WithAlpha(20), sc1.WithAlpha(80), sc1,
                    scCenter,
                    sc2, sc2.WithAlpha(80), sc2.WithAlpha(20),
                    SKColors.Transparent,
                };
                var shinePos = new float[]
                {
                    0f, 0.18f, 0.33f, 0.50f - innerSpread, 0.50f, 0.50f + innerSpread, 0.67f, 0.82f, 1f
                };

                using (var shinePaint = new SKPaint())
                {
                    shinePaint.IsAntialias = true;
                    shinePaint.BlendMode = blend;
                    shinePaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(barLeft, 0), new SKPoint(barRight, 0),
                        shineColors, shinePos, SKShaderTileMode.Clamp);
                    canvas.Save();
                    canvas.RotateDegrees(angle, w / 2f, h / 2f);
                    canvas.DrawRect(-w, -h, w * 3, h * 3, shinePaint);
                    canvas.Restore();
                }
            }

            // =============================================================
            //  LAYER 3: Metallic luster — applied AFTER shimmer bar so it
            //  modifies the final composited result (icon + shimmer).
            //  Per-pixel screen blend creates a moving highlight that
            //  visually interacts with both the icon and the shimmer bar.
            // =============================================================
            if (_effectShape == EffectShape.Circular)
            {
                if (t > 0.01)
                {
                    ApplyLuster(canvas, w, h, dir, t);
                    if (_logThrottle == 0)
                        Logger.Info($"[FX] ApplyLuster: dir={dir:F3} t={t:F3} shape={_effectShape}");
                }
            }
            else if (Math.Abs(dir) > 0.01 && t > 0.01)
            {
                ApplyLuster(canvas, w, h, dir, t);
                if (_logThrottle == 0)
                    Logger.Info($"[FX] ApplyLuster: dir={dir:F3} t={t:F3} shape={_effectShape}");
            }

            // =============================================================
            //  LAYER 4: Glint flash — brief bright overlay
            // =============================================================
            if (_glintEnabled && _glintPhase >= 0)
            {
                // Flash curve: quick ramp up, slower fade (0→1→0 over ~0.25s)
                double flashIntensity = _glintPhase < 0.3
                    ? _glintPhase / 0.3    // ramp up
                    : (1.0 - _glintPhase) / 0.7; // fade down
                flashIntensity = Math.Max(0, Math.Min(1, flashIntensity));
                byte flashAlpha = (byte)(200 * flashIntensity);

                using (var flashPaint = new SKPaint())
                {
                    flashPaint.IsAntialias = true;
                    flashPaint.BlendMode = SKBlendMode.SrcOver;
                    // Radial flash centered randomly-ish (uses current cycle phase)
                    float fx = w * (0.3f + (float)(_currentPhase * 0.4));
                    float fy = h * (0.3f + (float)((1.0 - _currentPhase) * 0.4));
                    flashPaint.Shader = SKShader.CreateRadialGradient(
                        new SKPoint(fx, fy), w * 0.6f,
                        new SKColor[]
                        {
                            new SKColor(255, 255, 240, flashAlpha),
                            new SKColor(255, 255, 220, (byte)(flashAlpha * 0.4)),
                            new SKColor(255, 255, 255, 0),
                        },
                        new float[] { 0f, 0.3f, 1f },
                        SKShaderTileMode.Clamp);
                    canvas.DrawRect(0, 0, w, h, flashPaint);
                }
            }

            // --- Copy pixels to WriteableBitmap ---

            if (_writeableBmp == null || _writeableBmp.PixelWidth != w || _writeableBmp.PixelHeight != h)
                _writeableBmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);

            var pixmap = _skiaSurface.PeekPixels();
            _writeableBmp.Lock();
            try
            {
                unsafe
                {
                    Buffer.MemoryCopy(
                        (void*)pixmap.GetPixels(),
                        (void*)_writeableBmp.BackBuffer,
                        _writeableBmp.BackBufferStride * h,
                        w * 4 * h);
                }
                _writeableBmp.AddDirtyRect(new Int32Rect(0, 0, w, h));
            }
            finally { _writeableBmp.Unlock(); }
            return _writeableBmp;
        }

        /// <summary>
        /// Reads pixels from effectSurface, multiplies each pixel's alpha by the luminance mask,
        /// then draws the result onto destCanvas with the specified blend mode.
        /// maskStrength: 1.0 = full masking (luster), 0.4 = 60% floor + 40% luminance (shine bar).
        /// </summary>
        private void ApplyMaskedEffect(SKCanvas destCanvas, SKSurface effectSurface, int w, int h,
            bool hasLumMask, double maskStrength, SKBlendMode blendMode)
        {
            var effPixmap = effectSurface.PeekPixels();
            var effPtr = effPixmap.GetPixels();
            int byteCount = w * h * 4;

            // Read effect pixels into a temp buffer so we can modify alpha
            byte[] effPixels = new byte[byteCount];
            System.Runtime.InteropServices.Marshal.Copy(effPtr, effPixels, 0, byteCount);

            if (hasLumMask)
            {
                for (int i = 0; i < w * h; i++)
                {
                    int off = i * 4 + 3; // alpha channel (BGRA)
                    byte origA = effPixels[off];
                    if (origA == 0) continue;

                    // maskStrength=1.0: alpha *= lum/255 (full masking)
                    // maskStrength=0.4: alpha *= (0.6 + 0.4*lum/255) (60% floor)
                    double lumFactor = _luminanceMap[i] / 255.0;
                    double mask = maskStrength < 1.0
                        ? (1.0 - maskStrength) + maskStrength * lumFactor
                        : lumFactor;
                    effPixels[off] = (byte)(origA * mask);
                }
            }

            // Draw masked effect onto destination
            var handle = System.Runtime.InteropServices.GCHandle.Alloc(
                effPixels, System.Runtime.InteropServices.GCHandleType.Pinned);
            try
            {
                using (var bmp = new SKBitmap())
                {
                    bmp.InstallPixels(
                        new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul),
                        handle.AddrOfPinnedObject(), w * 4);
                    using (var paint = new SKPaint())
                    {
                        paint.BlendMode = blendMode;
                        destCanvas.DrawBitmap(bmp, 0, 0, paint);
                    }
                }
            }
            finally { handle.Free(); }
        }

        // ----------------------------------------------------------------
        //  CLEANUP
        // ----------------------------------------------------------------

        public void ResetToNeutral()
        {
            if (_tiltSkew != null) _tiltSkew.AngleX = 0;
            if (_tiltScale != null) _tiltScale.ScaleY = 1.0;
            if (_tiltTranslate != null) _tiltTranslate.Y = 0;
            if (_shimmerOverlay != null) _shimmerOverlay.Opacity = 0;
            _hoverBlend = 0;
            _targetHoverBlend = 0;
        }

        public void RemoveAll(Grid grid)
        {
            RemoveShineSweep(grid);
            RemoveShimmer(grid);
        }

        // ----------------------------------------------------------------
        //  HOVER STATE
        // ----------------------------------------------------------------

        public void SetHoverState(double mx, double my, bool hovering)
        {
            _hoverMX = mx;
            _hoverMY = my;
            _targetHoverBlend = hovering ? 1.0 : 0.0;
        }

        public void ResetHoverState()
        {
            _hoverBlend = 0;
            _targetHoverBlend = 0;
            _hoverMX = 0;
            _hoverMY = 0;
            if (_tiltTranslate != null)
                _tiltTranslate.Y = 0;
        }

        public void MarkHoverEventsAttached(bool attached)
        {
            _hoverEventsAttached = attached;
        }

        public bool IsHoverEventsAttached => _hoverEventsAttached;

        public void SetBreathingEnabled(bool enabled)
        {
            _breathingEnabled = enabled;
        }

        public void SetLevitation(bool enabled, double speed, double amount)
        {
            _levitationEnabled = enabled;
            _levitationSpeed = speed;
            _levitationAmount = amount;
        }

        public void Set3DRotation(bool enabled, double speed, double amount)
        {
            _3dRotationEnabled = enabled;
            _rotationSpeed = speed;
            _rotationAmount = amount;
        }

        public void SetGlint(bool enabled, double intervalMin, double intervalMax)
        {
            _glintEnabled = enabled;
            _glintIntervalMin = intervalMin;
            _glintIntervalMax = intervalMax;
            if (enabled && _glintNextTime <= 0)
                _glintNextTime = (DateTime.UtcNow - _breathStartTime).TotalSeconds
                    + intervalMin + Rng.NextDouble() * (intervalMax - intervalMin);
        }

        public void SetShadowDrift(bool enabled) { _shadowDriftEnabled = enabled; }
        public void SetParallax(bool enabled) { _parallaxEnabled = enabled; }

        /// <summary>Returns the current tilt X angle for shadow drift / parallax.</summary>
        public double GetCurrentTiltX()
        {
            return _tiltSkew?.AngleX ?? 0;
        }

        /// <summary>
        /// Creates the TransformGroup on the grid for hover/breathing when shimmer is off.
        /// </summary>
        public void EnsureTransforms(Grid grid, Image icon)
        {
            if (_tiltSkew != null && _tiltScale != null && _tiltTranslate != null) return;

            double w = icon.ActualWidth > 0 ? icon.ActualWidth : 64;
            double h = icon.ActualHeight > 0 ? icon.ActualHeight : 64;

            _tiltTranslate = new TranslateTransform(0, 0);
            _tiltSkew = new SkewTransform(0, 0, w / 2, h / 2);
            _tiltScale = new ScaleTransform(1, 1, w / 2, h / 2);
            var group = new TransformGroup();
            group.Children.Add(_tiltTranslate);
            group.Children.Add(_tiltSkew);
            group.Children.Add(_tiltScale);
            grid.RenderTransform = group;
        }

        // ----------------------------------------------------------------
        //  METALLIC LUSTER
        // ----------------------------------------------------------------

        /// <summary>
        /// Metallic luster: per-pixel directional contrast + saturation boost.
        /// The whole icon surface is affected — lit side gets higher contrast
        /// and more vivid colors, shadow side dims and desaturates.
        /// Applied AFTER the shimmer bar so the effect is always visible.
        /// </summary>
        private void ApplyLuster(SKCanvas canvas, int w, int h, double dir, double t)
        {
            if (_effectShape == EffectShape.Circular)
            {
                ApplyLusterRadial(canvas, w, h, t);
                return;
            }

            double strength = Math.Abs(dir) * t;
            if (strength < 0.01) return;

            // Flush pending canvas draw commands so the pixels are available
            canvas.Flush();

            var pixmap2 = _skiaSurface.PeekPixels();
            var ptr2 = pixmap2.GetPixels();
            int byteCount2 = w * h * 4;
            byte[] px = new byte[byteCount2];
            System.Runtime.InteropServices.Marshal.Copy(ptr2, px, 0, byteCount2);

            for (int y = 0; y < h; y++)
            {
                double ny = (double)y / h;

                for (int x = 0; x < w; x++)
                {
                    int off = (y * w + x) * 4;
                    if (px[off + 3] == 0) continue;

                    double nx = (double)x / w;

                    // Light center moves across icon with tilt. At dir=1, light
                    // is at nx≈0.15 so most of the icon is lit. Center of icon
                    // is no longer a dead zone.
                    double lightCenter = 0.5 - dir * 0.35;
                    double distFromLight = nx - lightCenter;
                    double lightFactor = -distFromLight * Math.Sign(dir) * 3.0;
                    lightFactor += (0.5 - ny) * 0.2 * Math.Abs(dir);
                    lightFactor = Math.Max(-1.0, Math.Min(1.0, lightFactor));
                    lightFactor *= strength;

                    double b = px[off + 0] / 255.0;
                    double g = px[off + 1] / 255.0;
                    double r = px[off + 2] / 255.0;

                    // Style-based luster tint color (highlight and shadow tones)
                    double tintR, tintG, tintB;     // highlight tint
                    double shadowR, shadowG, shadowB; // shadow tint
                    switch (_shineStyle)
                    {
                        case ShineStyle.Gold:
                            tintR = 1.0; tintG = 0.85; tintB = 0.4;      // warm gold highlight
                            shadowR = 0.3; shadowG = 0.2; shadowB = 0.05; // deep bronze shadow
                            break;
                        case ShineStyle.Platinum:
                            tintR = 0.85; tintG = 0.9; tintB = 1.0;      // cool silver highlight
                            shadowR = 0.15; shadowG = 0.18; shadowB = 0.25; // blue-steel shadow
                            break;
                        case ShineStyle.Crimson:
                            tintR = 1.0; tintG = 0.5; tintB = 0.35;      // hot red-orange highlight
                            shadowR = 0.25; shadowG = 0.05; shadowB = 0.05; // deep red shadow
                            break;
                        case ShineStyle.Holographic:
                            // Rainbow shift based on position — hue varies across icon
                            double hue = (nx + ny * 0.5 + lightFactor * 0.3) * 360.0 % 360.0;
                            if (hue < 0) hue += 360;
                            var hc = HsvToColor(hue, 0.6, 1.0);
                            tintR = hc.R / 255.0; tintG = hc.G / 255.0; tintB = hc.B / 255.0;
                            shadowR = 0.1; shadowG = 0.1; shadowB = 0.15;
                            break;
                        case ShineStyle.IconColors:
                            // Use the icon's extracted colors
                            tintR = _shimColor1.R / 255.0 * 0.5 + 0.5;
                            tintG = _shimColor1.G / 255.0 * 0.5 + 0.5;
                            tintB = _shimColor1.B / 255.0 * 0.5 + 0.5;
                            shadowR = _shimColor2.R / 255.0 * 0.3;
                            shadowG = _shimColor2.G / 255.0 * 0.3;
                            shadowB = _shimColor2.B / 255.0 * 0.3;
                            break;
                        default: // White
                            tintR = 1.0; tintG = 1.0; tintB = 1.0;       // pure white highlight
                            shadowR = 0.12; shadowG = 0.12; shadowB = 0.15; // neutral dark shadow
                            break;
                    }

                    if (lightFactor > 0)
                    {
                        // Saturation boost on original colors
                        double lum = 0.299 * r + 0.587 * g + 0.114 * b;
                        double satBoost = 1.0 + lightFactor * 2.5;
                        r = lum + (r - lum) * satBoost;
                        g = lum + (g - lum) * satBoost;
                        b = lum + (b - lum) * satBoost;
                        r = Math.Max(0, r);
                        g = Math.Max(0, g);
                        b = Math.Max(0, b);

                        // Contrast
                        double contrast = 1.0 + lightFactor * 1.4;
                        r = (r - 0.5) * contrast + 0.5;
                        g = (g - 0.5) * contrast + 0.5;
                        b = (b - 0.5) * contrast + 0.5;

                        // Screen-blend brightness
                        double lift = lightFactor * 0.90;
                        r = 1.0 - (1.0 - Math.Max(0, r)) * (1.0 - lift);
                        g = 1.0 - (1.0 - Math.Max(0, g)) * (1.0 - lift);
                        b = 1.0 - (1.0 - Math.Max(0, b)) * (1.0 - lift);

                        // Tint toward the style's highlight color at peak light
                        // White style keeps subtle, other styles are aggressive
                        double tintAmount = _shineStyle == ShineStyle.White
                            ? lightFactor * 0.25
                            : lightFactor * 0.65;
                        r = r * (1.0 - tintAmount) + tintR * tintAmount;
                        g = g * (1.0 - tintAmount) + tintG * tintAmount;
                        b = b * (1.0 - tintAmount) + tintB * tintAmount;
                    }
                    else
                    {
                        // Shadow side: dim + desaturate
                        double dim = 1.0 + lightFactor * 0.45;
                        r *= dim; g *= dim; b *= dim;

                        double lum = 0.299 * r + 0.587 * g + 0.114 * b;
                        double desat = 1.0 + lightFactor * 0.35;
                        r = lum + (r - lum) * desat;
                        g = lum + (g - lum) * desat;
                        b = lum + (b - lum) * desat;

                        // Tint toward the style's shadow color in deep shadow
                        double shadowTint = _shineStyle == ShineStyle.White
                            ? Math.Abs(lightFactor) * 0.15
                            : Math.Abs(lightFactor) * 0.45;
                        r = r * (1.0 - shadowTint) + shadowR * shadowTint;
                        g = g * (1.0 - shadowTint) + shadowG * shadowTint;
                        b = b * (1.0 - shadowTint) + shadowB * shadowTint;
                    }

                    px[off + 0] = (byte)Math.Max(0, Math.Min(255, b * 255));
                    px[off + 1] = (byte)Math.Max(0, Math.Min(255, g * 255));
                    px[off + 2] = (byte)Math.Max(0, Math.Min(255, r * 255));
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(px, 0, ptr2, byteCount2);
        }

        /// <summary>
        /// Radial luster — gradient-based, orbiting highlight with color-dodge.
        /// </summary>
        private void ApplyLusterRadial(SKCanvas canvas, int w, int h, double t)
        {
            if (t < 0.01) return;

            double orbitAngle = _orbitOffset + _currentPhase * 2.0 * Math.PI;

            if (_hoverBlend > 0.01)
            {
                double hoverAngle = Math.Atan2(_hoverMY, _hoverMX);
                double delta = hoverAngle - orbitAngle;
                while (delta > Math.PI) delta -= 2.0 * Math.PI;
                while (delta < -Math.PI) delta += 2.0 * Math.PI;
                orbitAngle = orbitAngle + delta * _hoverBlend;
            }

            // Tighter orbit + gradient to stay within circular icon bounds
            float lightX = (float)(w * (0.5 + Math.Cos(orbitAngle) * 0.15));
            float lightY = (float)(h * (0.5 + Math.Sin(orbitAngle) * 0.15));
            float gradRadius = Math.Min(w, h) * 0.45f;

            // Clip to circular icon shape
            canvas.Save();
            using (var clipPath = new SKPath())
            {
                float clipRadius = Math.Min(w, h) * 0.48f;
                clipPath.AddCircle(w / 2f, h / 2f, clipRadius);
                canvas.ClipPath(clipPath);
            }

            // Color-dodge: orbiting glow within circular bounds
            byte dodgeAlpha = (byte)Math.Min(255, 255 * t);

            using (var dodgePaint = new SKPaint())
            {
                dodgePaint.IsAntialias = true;
                dodgePaint.BlendMode = SKBlendMode.ColorDodge;
                dodgePaint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(lightX, lightY), gradRadius,
                    new SKColor[]
                    {
                        new SKColor(220, 220, 230, dodgeAlpha),
                        new SKColor(150, 150, 160, (byte)(dodgeAlpha * 0.5)),
                        new SKColor(40, 40, 45, (byte)(dodgeAlpha * 0.1)),
                        new SKColor(0, 0, 0, 0),
                    },
                    new float[] { 0f, 0.3f, 0.6f, 1f },
                    SKShaderTileMode.Clamp);
                canvas.DrawRect(0, 0, w, h, dodgePaint);
            }

            // Overlay for contrast
            byte overlayAlpha = (byte)Math.Min(255, 200 * t);

            using (var overlayPaint = new SKPaint())
            {
                overlayPaint.IsAntialias = true;
                overlayPaint.BlendMode = SKBlendMode.Overlay;
                overlayPaint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(lightX, lightY), gradRadius,
                    new SKColor[]
                    {
                        new SKColor(255, 255, 255, overlayAlpha),
                        new SKColor(180, 180, 190, (byte)(overlayAlpha * 0.4)),
                        new SKColor(0, 0, 0, (byte)(overlayAlpha * 0.5)),
                    },
                    new float[] { 0f, 0.35f, 1f },
                    SKShaderTileMode.Clamp);
                canvas.DrawRect(0, 0, w, h, overlayPaint);
            }

            canvas.Restore();
        }

        // ----------------------------------------------------------------
        //  LUMINANCE MAP HELPERS
        // ----------------------------------------------------------------

        /// <summary>
        /// In-place box blur of _luminanceMap to soften mask edges.
        /// Two-pass separable blur (horizontal then vertical).
        /// </summary>
        private void BlurLuminanceMap(int w, int h, int radius)
        {
            if (_luminanceMap == null || _luminanceMap.Length != w * h) return;
            var temp = new byte[w * h];

            // Horizontal pass
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int sum = 0, count = 0;
                    int x0 = Math.Max(0, x - radius);
                    int x1 = Math.Min(w - 1, x + radius);
                    for (int kx = x0; kx <= x1; kx++)
                    {
                        sum += _luminanceMap[y * w + kx];
                        count++;
                    }
                    temp[y * w + x] = (byte)(sum / count);
                }
            }

            // Vertical pass
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int sum = 0, count = 0;
                    int y0 = Math.Max(0, y - radius);
                    int y1 = Math.Min(h - 1, y + radius);
                    for (int ky = y0; ky <= y1; ky++)
                    {
                        sum += temp[ky * w + x];
                        count++;
                    }
                    _luminanceMap[y * w + x] = (byte)(sum / count);
                }
            }
        }

        // ----------------------------------------------------------------
        //  COLOR HELPERS
        // ----------------------------------------------------------------

        private static Color Brighten(Color c, double amt)
        {
            return Color.FromRgb(
                (byte)Math.Min(255, c.R + (255 - c.R) * amt),
                (byte)Math.Min(255, c.G + (255 - c.G) * amt),
                (byte)Math.Min(255, c.B + (255 - c.B) * amt));
        }

        private static Color Saturate(Color c, double amt)
        {
            double gray = (c.R + c.G + c.B) / 3.0;
            return Color.FromRgb(
                (byte)Math.Max(0, Math.Min(255, c.R + (c.R - gray) * amt)),
                (byte)Math.Max(0, Math.Min(255, c.G + (c.G - gray) * amt)),
                (byte)Math.Max(0, Math.Min(255, c.B + (c.B - gray) * amt)));
        }

        private static Color HsvToColor(double hue, double sat, double val)
        {
            double c = val * sat;
            double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            double m = val - c;
            double r, g, b;
            if (hue < 60)       { r = c; g = x; b = 0; }
            else if (hue < 120) { r = x; g = c; b = 0; }
            else if (hue < 180) { r = 0; g = c; b = x; }
            else if (hue < 240) { r = 0; g = x; b = c; }
            else if (hue < 300) { r = x; g = 0; b = c; }
            else                { r = c; g = 0; b = x; }
            return Color.FromRgb(
                (byte)Math.Min(255, (r + m) * 255),
                (byte)Math.Min(255, (g + m) * 255),
                (byte)Math.Min(255, (b + m) * 255));
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
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;
            double r1, g1, b1;
            if (h < 60)       { r1 = c; g1 = x;  b1 = 0; }
            else if (h < 120) { r1 = x;  g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0;  g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0;  g1 = x;  b1 = c; }
            else if (h < 300) { r1 = x;  g1 = 0;  b1 = c; }
            else              { r1 = c; g1 = 0;  b1 = x; }
            return Color.FromRgb(
                (byte)Math.Min(255, (r1 + m) * 255),
                (byte)Math.Min(255, (g1 + m) * 255),
                (byte)Math.Min(255, (b1 + m) * 255));
        }
    }
}
