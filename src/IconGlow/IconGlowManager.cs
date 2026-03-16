using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace BeautyCons.IconGlow
{
    public class IconGlowManager
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private readonly BeautyConsSettingsViewModel _settingsViewModel;
        private readonly IconColorExtractor _colorExtractor = new IconColorExtractor();
        private readonly GlowCache _glowCache;

        private BeautyConsSettings Settings => _settingsViewModel.Settings;

        // Visual tree state
        private Image _currentIcon;
        private Image _currentGlowImage;
        private Grid _currentWrapperGrid;
        private Panel _currentParentPanel;
        private int _originalIconIndex;
        private int _renderVersion;
        private Game _activeGame;

        // Pending game — queued during fade-out, applied when fade completes
        private Game _pendingGame;

        // Animation state (unified timer for spin, pulse, sparkles)
        private DispatcherTimer _animTimer;
        private RotateTransform _glowRotate;
        private double _glowAngle;
        private DateTime _animStartTime;
        private SparkleOverlay _sparkleOverlay;
        private Color _activeColor1, _activeColor2;

        // Color cycle state
        private DateTime _lastColorCycleTime;
        private double _colorCycleHueOffset;

        // Crossfade state
        private DispatcherTimer _fadeTimer;
        private Image _fadingOutGlowImage;
        private const double FadeDurationMs = 100.0;
        private const double FadeIntervalMs = 16.0;
        private double _fadeOutOpacity;
        private double _fadeInOpacity;
        private bool _isFadingIn;

        // Track which settings object we're subscribed to
        private BeautyConsSettings _subscribedSettings;

        public IconGlowManager(BeautyConsSettingsViewModel settingsViewModel, string extensionsDataPath)
        {
            _settingsViewModel = settingsViewModel;
            _glowCache = new GlowCache(extensionsDataPath);
            _animStartTime = DateTime.UtcNow;
            _lastColorCycleTime = _animStartTime;

            _settingsViewModel.PropertyChanged += OnViewModelPropertyChanged;
            SubscribeToSettings(Settings);
        }

        private void SubscribeToSettings(BeautyConsSettings settings)
        {
            if (_subscribedSettings != null)
                _subscribedSettings.PropertyChanged -= OnSettingsPropertyChanged;

            _subscribedSettings = settings;
            if (_subscribedSettings != null)
                _subscribedSettings.PropertyChanged += OnSettingsPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BeautyConsSettingsViewModel.Settings))
            {
                SubscribeToSettings(Settings);

                if (_activeGame != null)
                {
                    Application.Current?.Dispatcher?.BeginInvoke(
                        DispatcherPriority.Loaded,
                        new Action(() => ApplyGlow(_activeGame)));
                }
            }
        }

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(BeautyConsSettings.EnableIconGlow):
                    if (!Settings.EnableIconGlow)
                        Application.Current?.Dispatcher?.BeginInvoke(
                            DispatcherPriority.Loaded, new Action(() => RemoveGlow()));
                    else if (_activeGame != null)
                        Application.Current?.Dispatcher?.BeginInvoke(
                            DispatcherPriority.Loaded,
                            new Action(() => ApplyGlow(_activeGame)));
                    break;

                case nameof(BeautyConsSettings.IconGlowStyle):
                case nameof(BeautyConsSettings.IconGlowSize):
                case nameof(BeautyConsSettings.IconGlowIntensity):
                case nameof(BeautyConsSettings.ColorPreset):
                case nameof(BeautyConsSettings.CustomColor1):
                case nameof(BeautyConsSettings.CustomColor2):
                    _glowCache.InvalidateForSettings();
                    if (Settings.EnableIconGlow && _activeGame != null)
                    {
                        Application.Current?.Dispatcher?.BeginInvoke(
                            DispatcherPriority.Loaded,
                            new Action(() => ApplyGlow(_activeGame)));
                    }
                    break;

                case nameof(BeautyConsSettings.EnableIconGlowSpin):
                case nameof(BeautyConsSettings.IconGlowSpinSpeed):
                case nameof(BeautyConsSettings.EnablePulse):
                case nameof(BeautyConsSettings.PulseSpeed):
                case nameof(BeautyConsSettings.PulseMinOpacity):
                case nameof(BeautyConsSettings.EnableSparkles):
                case nameof(BeautyConsSettings.SparkleCount):
                case nameof(BeautyConsSettings.SparkleSpeed):
                case nameof(BeautyConsSettings.EnableColorCycle):
                case nameof(BeautyConsSettings.ColorCycleSpeed):
                    Application.Current?.Dispatcher?.BeginInvoke(
                        DispatcherPriority.Loaded,
                        new Action(() => UpdateAnimationState()));
                    break;
            }
        }

        public void OnGameSelected(Game game)
        {
            if (game == null || !Settings.EnableIconGlow)
            {
                _activeGame = null;
                _pendingGame = null;
                Application.Current?.Dispatcher?.BeginInvoke(
                    DispatcherPriority.Loaded,
                    new Action(() => RemoveGlow()));
                return;
            }

            _activeGame = game;

            Application.Current?.Dispatcher?.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    if (_currentGlowImage != null)
                    {
                        // Stop animations and fade out from current opacity (no snap)
                        StopAnimTimer();
                        Logger.Info($"[BeautyCons] GameSwitch: stopped anim, fading out from opacity {_currentGlowImage.Opacity:F2}");
                        _pendingGame = game;
                        StartFadeOut();
                    }
                    else
                    {
                        Logger.Info($"[BeautyCons] GameSwitch: no existing glow, applying directly");
                        // No existing glow — apply directly
                        ApplyGlow(game);
                    }
                }));
        }

        private void StartFadeOut()
        {
            StopFadeTimer();

            if (_currentGlowImage == null)
            {
                Logger.Info("[BeautyCons] StartFadeOut: no glow image, skipping");
                // No glow to fade — apply pending game immediately
                if (_pendingGame != null)
                {
                    var game = _pendingGame;
                    _pendingGame = null;
                    ApplyGlow(game);
                }
                return;
            }

            _fadingOutGlowImage = _currentGlowImage;
            _fadeOutOpacity = _fadingOutGlowImage.Opacity;
            Logger.Info($"[BeautyCons] StartFadeOut: starting from opacity {_fadeOutOpacity:F2}");

            _fadeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(FadeIntervalMs)
            };
            _fadeTimer.Tick += OnFadeTick;
            _fadeTimer.Start();
        }

        private void OnFadeTick(object sender, EventArgs e)
        {
            double step = FadeIntervalMs / FadeDurationMs;

            // Fade out old glow
            if (_fadingOutGlowImage != null)
            {
                _fadeOutOpacity -= step;
                if (_fadeOutOpacity <= 0)
                {
                    _fadingOutGlowImage.Opacity = 0;
                    _fadingOutGlowImage = null;

                    Logger.Info("[BeautyCons] FadeOut complete");
                    // Fade-out complete — apply pending game if queued
                    if (_pendingGame != null)
                    {
                        var game = _pendingGame;
                        _pendingGame = null;
                        Logger.Info($"[BeautyCons] Applying pending game: {game.Name}");
                        StopFadeTimer();
                        ApplyGlow(game);
                        return;
                    }
                }
                else
                {
                    _fadingOutGlowImage.Opacity = _fadeOutOpacity;
                }
            }

            // Fade in new glow
            // Fade in new glow
            if (_isFadingIn && _currentGlowImage != null)
            {
                _fadeInOpacity += step;
                if (_fadeInOpacity >= 1.0)
                {
                    _currentGlowImage.Opacity = 1.0;
                    _isFadingIn = false;

                    // Fade-in complete — now start animations
                    // Reset pulse phase so it starts at peak (opacity 1.0)
                    // sin(π/2) = 1.0, so offset start time by PulseSpeed/4 into the past
                    _animStartTime = DateTime.UtcNow - TimeSpan.FromSeconds(Settings.PulseSpeed / 4.0);
                    Logger.Info("[BeautyCons] FadeIn complete, starting animations from pulse peak");
                    UpdateAnimationState();
                }
                else
                {
                    _currentGlowImage.Opacity = _fadeInOpacity;
                }
            }

            if (_fadingOutGlowImage == null && !_isFadingIn)
            {
                StopFadeTimer();
            }
        }

        private void StartFadeIn()
        {
            if (_currentGlowImage == null) return;

            Logger.Info("[BeautyCons] StartFadeIn: opacity → 0, beginning ramp up");
            _isFadingIn = true;
            _fadeInOpacity = 0;
            _currentGlowImage.Opacity = 0;

            if (_fadeTimer == null)
            {
                _fadeTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(FadeIntervalMs)
                };
                _fadeTimer.Tick += OnFadeTick;
                _fadeTimer.Start();
            }
        }

        private void StopFadeTimer()
        {
            if (_fadeTimer == null) return;
            _fadeTimer.Stop();
            _fadeTimer.Tick -= OnFadeTick;
            _fadeTimer = null;
            _fadingOutGlowImage = null;
            _isFadingIn = false;
        }

        private void ApplyGlow(Game game)
        {
            if (game == null) return;

            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow == null) return;

            var icon = TileFinder.FindSelectedGameIcon(mainWindow);
            if (icon == null || icon.ActualWidth <= 0 || icon.ActualHeight <= 0)
                return;

            // Resolve colors from preset
            Color color1, color2;
            switch (Settings.ColorPreset)
            {
                case ColorPreset.Auto:
                    var extracted = _colorExtractor.GetGlowColors(game.Id, icon.Source);
                    color1 = extracted.primary;
                    color2 = extracted.secondary;
                    break;
                case ColorPreset.Custom:
                    color1 = ParseHexColor(Settings.CustomColor1, Color.FromRgb(100, 149, 237));
                    color2 = ParseHexColor(Settings.CustomColor2, Color.FromRgb(180, 100, 255));
                    break;
                default:
                    var presetColors = ColorPresets.GetColors(Settings.ColorPreset);
                    color1 = presetColors.primary;
                    color2 = presetColors.secondary;
                    break;
            }

            // Apply color cycle hue offset if active
            if (Settings.EnableColorCycle && _colorCycleHueOffset > 0)
            {
                color1 = ShiftHue(color1, _colorCycleHueOffset);
                color2 = ShiftHue(color2, _colorCycleHueOffset);
            }

            // Store active colors for animations (sparkles)
            _activeColor1 = color1;
            _activeColor2 = color2;

            var styleParams = GlowStyleParams.GetParams(Settings.IconGlowStyle);
            double baseSigma = Settings.IconGlowSize;
            double intensity = Settings.IconGlowIntensity;
            double displayWidth = icon.ActualWidth;
            double displayHeight = icon.ActualHeight;

            string cacheKey = GlowCache.BuildCacheKey(game.Id, Settings.IconGlowStyle,
                baseSigma, intensity, Settings.ColorPreset == ColorPreset.Custom,
                Settings.CustomColor1, Settings.CustomColor2, color1, color2);

            // Memory cache hit — instant apply with fade-in
            var cached = _glowCache.TryGetMemory(cacheKey);
            if (cached != null)
            {
                Logger.Info("[BeautyCons] Memory cache HIT");
                // Clear any lingering fade-out reference so it doesn't fight with fade-in
                _fadingOutGlowImage = null;
                ApplyGlowToIcon(icon, cached, styleParams, baseSigma);
                return;
            }
            Logger.Info("[BeautyCons] Memory cache MISS, checking disk/rendering");

            // Extract pixels on UI thread
            byte[] pixels = null;
            int srcWidth = 0, srcHeight = 0;
            try
            {
                var bitmapSource = icon.Source as BitmapSource;
                if (bitmapSource != null)
                {
                    if (bitmapSource.Format != PixelFormats.Bgra32 && bitmapSource.Format != PixelFormats.Pbgra32)
                        bitmapSource = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgra32, null, 0);

                    srcWidth = bitmapSource.PixelWidth;
                    srcHeight = bitmapSource.PixelHeight;
                    int stride = srcWidth * 4;
                    pixels = new byte[srcHeight * stride];
                    bitmapSource.CopyPixels(pixels, stride, 0);
                }
            }
            catch { }

            _renderVersion++;
            int myVersion = _renderVersion;

            Task.Run(() =>
            {
                var diskCached = _glowCache.TryLoadFromDisk(cacheKey);
                if (diskCached != null)
                {
                    Application.Current?.Dispatcher?.BeginInvoke(
                        DispatcherPriority.Loaded,
                        new Action(() =>
                        {
                            if (myVersion != _renderVersion) return;
                            Logger.Info("[BeautyCons] Disk cache HIT");
                            _fadingOutGlowImage = null;
                            ApplyGlowToIcon(icon, diskCached, styleParams, baseSigma);
                        }));
                    return;
                }

                if (pixels == null) return;

                var glowBitmap = GlowRenderer.RenderGlow(
                    pixels, srcWidth, srcHeight,
                    color1, color2,
                    styleParams, baseSigma,
                    displayWidth, displayHeight,
                    intensity);

                if (glowBitmap == null) return;

                _glowCache.Store(cacheKey, glowBitmap);

                Application.Current?.Dispatcher?.BeginInvoke(
                    DispatcherPriority.Loaded,
                    new Action(() =>
                    {
                        if (myVersion != _renderVersion) return;
                        Logger.Info("[BeautyCons] Render complete, applying");
                        _fadingOutGlowImage = null;
                        ApplyGlowToIcon(icon, glowBitmap, styleParams, baseSigma);
                    }));
            });
        }

        private void ApplyGlowToIcon(Image icon, BitmapSource glowBitmap, GlowStyleParams styleParams, double baseSigma)
        {
            double extend = GlowRenderer.CalculateExtend(styleParams, baseSigma);

            // If same icon element is already wrapped, just swap the bitmap source
            // This avoids removing/inserting elements which causes layout flicker
            if (_currentIcon == icon && _currentGlowImage != null && _currentWrapperGrid != null
                && _currentParentPanel != null && _currentParentPanel.Children.Contains(_currentWrapperGrid))
            {
                Logger.Info("[BeautyCons] ApplyGlowToIcon: reusing wrapper, swapping source");
                _currentGlowImage.Source = glowBitmap;
                _currentGlowImage.Width = icon.ActualWidth + extend * 2;
                _currentGlowImage.Height = icon.ActualHeight + extend * 2;
                _currentGlowImage.Margin = new Thickness(-extend);
                StartFadeIn();
                return;
            }

            var glowImage = GlowRenderer.CreateGlowImage(glowBitmap, icon.ActualWidth, icon.ActualHeight, extend);

            _glowRotate = new RotateTransform(_glowAngle);
            glowImage.RenderTransform = _glowRotate;
            glowImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

            // Remove any existing glow first
            RemoveGlowVisuals();

            var parent = VisualTreeHelper.GetParent(icon) as Panel;
            if (parent == null) return;

            int iconIndex = parent.Children.IndexOf(icon);
            if (iconIndex < 0) return;

            _currentIcon = icon;
            _currentParentPanel = parent;
            _originalIconIndex = iconIndex;

            var wrapper = new Grid();
            wrapper.ClipToBounds = false;
            wrapper.Margin = icon.Margin;
            icon.Margin = new Thickness(0);

            if (parent is DockPanel)
            {
                DockPanel.SetDock(wrapper, DockPanel.GetDock(icon));
            }

            parent.Children.RemoveAt(iconIndex);
            wrapper.Children.Add(glowImage);
            wrapper.Children.Add(icon);
            parent.Children.Insert(Math.Min(iconIndex, parent.Children.Count), wrapper);

            _currentWrapperGrid = wrapper;
            _currentGlowImage = glowImage;

            StartFadeIn();
        }

        private void RemoveGlowVisuals()
        {
            bool wrapperIsLive = _currentWrapperGrid != null && _currentParentPanel != null
                              && _currentParentPanel.Children.Contains(_currentWrapperGrid);

            if (wrapperIsLive && _currentIcon != null)
            {
                try
                {
                    // Detach sparkle canvas before clearing children (preserve it for reuse)
                    if (_sparkleOverlay != null && _currentWrapperGrid.Children.Contains(_sparkleOverlay.Canvas))
                        _currentWrapperGrid.Children.Remove(_sparkleOverlay.Canvas);

                    _currentIcon.Margin = _currentWrapperGrid.Margin;
                    _currentWrapperGrid.Children.Clear();
                    _currentParentPanel.Children.Remove(_currentWrapperGrid);
                    _currentParentPanel.Children.Insert(
                        Math.Min(_originalIconIndex, _currentParentPanel.Children.Count),
                        _currentIcon);
                }
                catch { }
            }

            _currentGlowImage = null;
            _currentWrapperGrid = null;
            _currentIcon = null;
            _currentParentPanel = null;
            _glowRotate = null;
        }

        private void UpdateAnimationState()
        {
            bool needsAnimation = _currentGlowImage != null && (
                Settings.EnableIconGlowSpin ||
                Settings.EnablePulse ||
                Settings.EnableSparkles ||
                Settings.EnableColorCycle);

            if (needsAnimation)
            {
                if (Settings.EnableSparkles && _currentIcon != null)
                {
                    var styleParams = GlowStyleParams.GetParams(Settings.IconGlowStyle);
                    double extend = GlowRenderer.CalculateExtend(styleParams, Settings.IconGlowSize);

                    if (_sparkleOverlay == null)
                    {
                        _sparkleOverlay = new SparkleOverlay();
                        _sparkleOverlay.Configure(
                            _currentIcon.ActualWidth, _currentIcon.ActualHeight, extend,
                            _activeColor1, _activeColor2, Settings.SparkleCount);
                    }

                    // Re-parent canvas into current wrapper if needed
                    if (_currentWrapperGrid != null && !_currentWrapperGrid.Children.Contains(_sparkleOverlay.Canvas))
                    {
                        _sparkleOverlay.Canvas.Width = _currentIcon.ActualWidth + extend * 2;
                        _sparkleOverlay.Canvas.Height = _currentIcon.ActualHeight + extend * 2;
                        _sparkleOverlay.Canvas.Margin = new Thickness(-extend);
                        _sparkleOverlay.Canvas.HorizontalAlignment = HorizontalAlignment.Center;
                        _sparkleOverlay.Canvas.VerticalAlignment = VerticalAlignment.Center;
                        _currentWrapperGrid.Children.Add(_sparkleOverlay.Canvas);
                    }
                }
                else if (!Settings.EnableSparkles && _sparkleOverlay != null)
                {
                    RemoveSparkleOverlay();
                }

                StartAnimTimer();
            }
            else
            {
                StopAnimTimer();
                RemoveSparkleOverlay();
                if (!Settings.EnableIconGlowSpin)
                {
                    _glowAngle = 0;
                    if (_glowRotate != null)
                        _glowRotate.Angle = 0;
                }
                if (!Settings.EnablePulse && _currentGlowImage != null)
                    _currentGlowImage.Opacity = 1.0;
            }
        }

        private void RemoveSparkleOverlay()
        {
            if (_sparkleOverlay != null)
            {
                if (_currentWrapperGrid != null && _currentWrapperGrid.Children.Contains(_sparkleOverlay.Canvas))
                    _currentWrapperGrid.Children.Remove(_sparkleOverlay.Canvas);
                _sparkleOverlay.Clear();
                _sparkleOverlay = null;
            }
        }

        private void StartAnimTimer()
        {
            if (_animTimer != null) return;

            _animTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps
            };
            _animTimer.Tick += OnAnimTick;
            _animTimer.Start();
        }

        private void StopAnimTimer()
        {
            if (_animTimer == null) return;
            _animTimer.Stop();
            _animTimer.Tick -= OnAnimTick;
            _animTimer = null;
        }

        private void OnAnimTick(object sender, EventArgs e)
        {
            if (_currentGlowImage == null)
            {
                StopAnimTimer();
                return;
            }

            double elapsed = (DateTime.UtcNow - _animStartTime).TotalSeconds;

            // Spin
            if (Settings.EnableIconGlowSpin && _glowRotate != null)
            {
                double degreesPerFrame = 360.0 / (Settings.IconGlowSpinSpeed * 60.0);
                _glowAngle = (_glowAngle + degreesPerFrame) % 360.0;
                _glowRotate.Angle = _glowAngle;
            }

            // Pulse (sine wave opacity breathing)
            if (Settings.EnablePulse)
            {
                double sine = Math.Sin(elapsed * 2 * Math.PI / Settings.PulseSpeed);
                double range = 1.0 - Settings.PulseMinOpacity;
                double opacity = Settings.PulseMinOpacity + (sine * 0.5 + 0.5) * range;
                _currentGlowImage.Opacity = Math.Max(0, Math.Min(1, opacity));
            }

            // Color cycle — periodically re-render with shifted hue
            if (Settings.EnableColorCycle)
            {
                double cycleElapsed = (DateTime.UtcNow - _lastColorCycleTime).TotalSeconds;
                if (cycleElapsed >= 1.5) // re-render every 1.5 seconds
                {
                    _lastColorCycleTime = DateTime.UtcNow;
                    _colorCycleHueOffset = (_colorCycleHueOffset + 360.0 / Settings.ColorCycleSpeed * 1.5) % 360.0;

                    if (_activeGame != null)
                    {
                        _glowCache.InvalidateForSettings();
                        ApplyGlow(_activeGame);
                    }
                }
            }

            // Sparkles
            if (Settings.EnableSparkles && _sparkleOverlay != null)
            {
                _sparkleOverlay.Update(Settings.SparkleSpeed);
            }
        }

        public void RemoveGlow()
        {
            StopAnimTimer();
            StopFadeTimer();
            RemoveSparkleOverlay();
            RemoveGlowVisuals();
            _glowAngle = 0;
        }

        public void Destroy()
        {
            _settingsViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            if (_subscribedSettings != null)
                _subscribedSettings.PropertyChanged -= OnSettingsPropertyChanged;
            _subscribedSettings = null;

            RemoveGlow();
            _colorExtractor.ClearCache();
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

            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;
            double r1, g1, b1;
            if (h < 60)       { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
            else              { r1 = c; g1 = 0; b1 = x; }

            return Color.FromRgb(
                (byte)Math.Min(255, (r1 + m) * 255),
                (byte)Math.Min(255, (g1 + m) * 255),
                (byte)Math.Min(255, (b1 + m) * 255));
        }

        private static Color ParseHexColor(string hex, Color fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hex)) return fallback;
                hex = hex.Trim();
                if (!hex.StartsWith("#")) hex = "#" + hex;
                var converted = ColorConverter.ConvertFromString(hex);
                if (converted is Color color) return color;
                return fallback;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
