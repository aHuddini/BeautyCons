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
        // Shine sweep (standalone effect)
        private Canvas _shineCanvas;
        private Rectangle _shineBar;
        private double _shineSweepPos;

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
        private double _cycleStart;
        private double _pauseUntil;
        private bool _paused;
        private bool _flipDir; // alternates sweep direction each cycle

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

        public void ApplyShineSweep(Grid grid, Image icon)
        {
            RemoveShineSweep(grid);
            if (icon == null) return;

            double w = icon.ActualWidth > 0 ? icon.ActualWidth : 64;
            double h = icon.ActualHeight > 0 ? icon.ActualHeight : 64;

            _shineCanvas = new Canvas
            {
                Width = w, Height = h,
                IsHitTestVisible = false, ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

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
            grid.Children.Add(_shineCanvas);
        }

        public void UpdateShineSweep(double speed)
        {
            if (_shineBar == null || _shineCanvas == null) return;
            double w = _shineCanvas.Width;
            double barW = _shineBar.Width;
            _shineSweepPos += (w + barW * 2) / (speed * 60.0);
            if (_shineSweepPos > w + barW) _shineSweepPos = -barW;
            Canvas.SetLeft(_shineBar, _shineSweepPos);
        }

        public void RemoveShineSweep(Grid grid)
        {
            if (_shineCanvas != null && grid != null && grid.Children.Contains(_shineCanvas))
                grid.Children.Remove(_shineCanvas);
            _shineCanvas = null;
            _shineBar = null;
        }

        // ----------------------------------------------------------------
        //  SHIMMER (includes tilt, luster, shine bar — one unified cycle)
        // ----------------------------------------------------------------

        public bool IsShimmerActive => _shimmerOverlay != null;

        public void ApplyShimmer(Grid grid, Image icon, double opacity,
            Color color1, Color color2, ShineStyle style, bool enableTilt, double now)
        {
            if (icon == null) return;

            // Already running on this grid — just update parameters
            if (_shimmerGrid == grid && _shimmerOverlay != null)
            {
                _shimColor1 = color1;
                _shimColor2 = color2;
                _shineStyle = style;
                _shimmerOpacity = opacity;
                _tiltEnabled = enableTilt;
                // Re-cache icon pixels in case the icon source changed (game switch)
                CacheIconPixels(icon);
                Logger.Info("[FX] ApplyShimmer: idempotent update (same grid)");
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

            // Set up tilt transforms on the grid
            _tiltSkew = new SkewTransform(0, 0, _iconW / 2, _iconH / 2);
            _tiltScale = new ScaleTransform(1, 1, _iconW / 2, _iconH / 2);
            var group = new TransformGroup();
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
                    if (_tiltSkew != null)
                    {
                        _tiltSkew.AngleX = 0;
                        _tiltScale.ScaleY = 1.0;
                    }
                    return;
                }
            }

            double elapsed = now - _cycleStart;
            double phase = elapsed / speed; // 0.0 → 1.0 over one cycle

            if (phase >= 1.0)
            {
                // Cycle complete — enter pause
                double pauseDur = pauseMin + Rng.NextDouble() * (pauseMax - pauseMin);
                _paused = true;
                _pauseUntil = now + pauseDur;
                _flipDir = !_flipDir; // alternate direction next cycle
                _shimmerOverlay.Opacity = 0;
                if (_tiltSkew != null)
                {
                    _tiltSkew.AngleX = 0;
                    _tiltScale.ScaleY = 1.0;
                }
                Logger.Info($"[FX] Cycle done at phase={phase:F3}, pausing {pauseDur:F1}s, flipDir={_flipDir}");
                return;
            }

            // --- Waveforms (all start and end at 0) ---
            // Shimmer intensity: sin(phase*PI) → 0→1→0
            double t = Math.Sin(phase * Math.PI);
            // Directional sweep: sin(phase*2*PI) → 0→+1→0→-1→0
            // Alternates direction each cycle so tilt doesn't always go the same way
            double dir = Math.Sin(phase * 2.0 * Math.PI) * (_flipDir ? -1.0 : 1.0);

            // Tilt follows dir (starts at 0, swings right, returns, swings left, returns to 0)
            if (_tiltEnabled && _tiltSkew != null)
            {
                _tiltSkew.AngleX = dir * MaxSkewDeg;
                _tiltScale.ScaleY = 1.0 - Math.Abs(dir) * MaxScaleY;
            }
            else if (_tiltSkew != null)
            {
                _tiltSkew.AngleX = 0;
                _tiltScale.ScaleY = 1.0;
            }

            // Diagnostic logging (every ~1 second)
            _logThrottle++;
            if (_logThrottle >= 60)
            {
                _logThrottle = 0;
                Logger.Info($"[FX] phase={phase:F3} t={t:F3} dir={dir:F3} tilt={_tiltSkew?.AngleX:F2} opacity={_shimmerOverlay.Opacity:F3}");
            }

            // Render shimmer frame
            RenderFrame(t, dir);
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
            _paused = false;

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

                if (src.Format != PixelFormats.Bgra32 && src.Format != PixelFormats.Pbgra32)
                    src = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
                _pixW = src.PixelWidth;
                _pixH = src.PixelHeight;
                int pixelCount = _pixW * _pixH;
                _iconPixels = new byte[pixelCount * 4];
                src.CopyPixels(_iconPixels, _pixW * 4, 0);

                // Build luminance map with contrast stretch for content-aware masking
                _luminanceMap = new byte[pixelCount];
                for (int i = 0; i < pixelCount; i++)
                {
                    int off = i * 4; // BGRA
                    double rawLum = 0.114 * _iconPixels[off] + 0.587 * _iconPixels[off + 1] + 0.299 * _iconPixels[off + 2];
                    // Contrast stretch: push midtones apart so mask is more selective
                    double stretched = (rawLum - 64.0) * 1.5;
                    _luminanceMap[i] = (byte)Math.Max(0, Math.Min(255, stretched));
                }

                _cachedIconSource = icon.Source;
                Logger.Info($"[FX] CacheIconPixels: cached {_pixW}x{_pixH} with luminance map");
            }
            catch { _iconPixels = null; _luminanceMap = null; _pixW = _pixH = 0; _cachedIconSource = null; }
        }

        private void RenderFrame(double t, double dir)
        {
            if (_shimmerOverlay == null) return;

            // t is already a smooth sine (0→1→0), use it directly for opacity
            _shimmerOverlay.Opacity = _shimmerOpacity * t;

            // Skip rendering when nearly invisible — avoids flash on cycle restart
            if (t < 0.05)
                return;

            int w = _pixW > 0 ? _pixW : (int)_iconW;
            int h = _pixH > 0 ? _pixH : (int)_iconH;
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

            // --- Base icon pixels ---
            if (_iconPixels != null && _pixW == w && _pixH == h)
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
            //  LAYER 1: Metallic luster — luminance-masked (full masking)
            // =============================================================
            float lightCenter = (float)(0.5 - dir * 0.4);
            byte highlightA = (byte)(Math.Abs(dir) * 120 * t);
            byte subtleDarkA = (byte)(Math.Abs(dir) * 30 * t);

            var lusterColors = new SKColor[]
            {
                new SKColor(0, 0, 0, subtleDarkA),
                SKColors.Transparent,
                new SKColor(255, 255, 255, (byte)(highlightA * 0.4)),
                new SKColor(255, 255, 255, highlightA),
                new SKColor(255, 255, 255, (byte)(highlightA * 0.4)),
                SKColors.Transparent,
            };
            float hlLeft = Math.Max(0.01f, lightCenter - 0.25f);
            float hlRight = Math.Min(0.99f, lightCenter + 0.25f);
            var lusterStops = new float[]
            {
                0.00f,
                Math.Max(0.01f, hlLeft * 0.5f),
                hlLeft,
                lightCenter,
                hlRight,
                Math.Min(0.99f, hlRight + (1f - hlRight) * 0.5f)
            };
            // Ensure monotonically increasing
            for (int i = 1; i < lusterStops.Length; i++)
                if (lusterStops[i] <= lusterStops[i - 1])
                    lusterStops[i] = lusterStops[i - 1] + 0.001f;

            // Draw luster to effect surface, then mask and composite
            var effCanvas = _effectSurface.Canvas;
            effCanvas.Clear(SKColors.Transparent);
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(w, 0),
                    lusterColors, lusterStops, SKShaderTileMode.Clamp);
                effCanvas.DrawRect(0, 0, w, h, paint);
            }

            // Apply luminance mask to luster and composite onto main surface
            ApplyMaskedEffect(canvas, _effectSurface, w, h, hasLumMask, 1.0, SKBlendMode.SoftLight);

            // =============================================================
            //  LAYER 2: Shine bar — lightly luminance-masked (60% floor)
            // =============================================================
            double hueShift = dir * 15.0;
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

            // Bar geometry — driven by sweep direction
            double barAbsTilt = Math.Abs(dir);
            float barW = (float)(w * 0.55 * (1.0 + barAbsTilt * 0.5));
            float barX = (float)(w * 0.5 + (-dir) * w * 0.35);
            float angle = (float)(18.0 + dir * 8.0);
            float barLeft = barX - barW / 2;
            float barRight = barX + barW / 2;

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
                0f, 0.20f, 0.35f, 0.44f, 0.50f, 0.56f, 0.65f, 0.80f, 1f
            };

            // Draw shine bar directly onto main canvas (no masking)
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
            if (_shimmerOverlay != null) _shimmerOverlay.Opacity = 0;
        }

        public void RemoveAll(Grid grid)
        {
            RemoveShineSweep(grid);
            RemoveShimmer(grid);
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
