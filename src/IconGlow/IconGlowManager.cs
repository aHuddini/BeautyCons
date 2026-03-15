using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Playnite.SDK.Models;

namespace BeautyCons.IconGlow
{
    public class IconGlowManager
    {
        private readonly BeautyConsSettingsViewModel _settingsViewModel;
        private readonly IconColorExtractor _colorExtractor = new IconColorExtractor();

        private BeautyConsSettings Settings => _settingsViewModel.Settings;

        // Visual tree state
        private Image _currentIcon;
        private Image _currentGlowImage;
        private Grid _currentWrapperGrid;
        private Panel _currentParentPanel;
        private int _originalIconIndex;
        private int _renderVersion;
        private Game _activeGame;

        // Track which settings object we're subscribed to
        private BeautyConsSettings _subscribedSettings;

        public IconGlowManager(BeautyConsSettingsViewModel settingsViewModel)
        {
            _settingsViewModel = settingsViewModel;

            // Subscribe to ViewModel property changes (detects Settings object replacement from CancelEdit)
            _settingsViewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Subscribe to current Settings object property changes
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
                // Settings object was replaced (CancelEdit or reload) — re-subscribe
                SubscribeToSettings(Settings);

                // Re-apply glow with restored settings
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
                case nameof(BeautyConsSettings.UseCustomColors):
                case nameof(BeautyConsSettings.CustomColor1):
                case nameof(BeautyConsSettings.CustomColor2):
                    if (Settings.EnableIconGlow && _activeGame != null)
                    {
                        Application.Current?.Dispatcher?.BeginInvoke(
                            DispatcherPriority.Loaded,
                            new Action(() => ApplyGlow(_activeGame)));
                    }
                    break;
            }
        }

        public void OnGameSelected(Game game)
        {
            if (game == null || !Settings.EnableIconGlow)
            {
                _activeGame = null;
                Application.Current?.Dispatcher?.BeginInvoke(
                    DispatcherPriority.Loaded,
                    new Action(() => RemoveGlow()));
                return;
            }

            _activeGame = game;

            Application.Current?.Dispatcher?.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() => ApplyGlow(game)));
        }

        private void ApplyGlow(Game game)
        {
            if (game == null) return;

            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow == null) return;

            var icon = TileFinder.FindSelectedGameIcon(mainWindow);
            if (icon == null || icon.ActualWidth <= 0 || icon.ActualHeight <= 0)
                return;

            // Get colors
            Color color1, color2;
            if (Settings.UseCustomColors)
            {
                color1 = ParseHexColor(Settings.CustomColor1, Color.FromRgb(100, 149, 237));
                color2 = ParseHexColor(Settings.CustomColor2, Color.FromRgb(180, 100, 255));
            }
            else
            {
                var extracted = _colorExtractor.GetGlowColors(game.Id, icon.Source);
                color1 = extracted.primary;
                color2 = extracted.secondary;
            }

            var styleParams = GlowStyleParams.GetParams(Settings.IconGlowStyle);
            double baseSigma = Settings.IconGlowSize;
            double intensity = Settings.IconGlowIntensity;
            double displayWidth = icon.ActualWidth;
            double displayHeight = icon.ActualHeight;

            // Extract pixels on UI thread
            byte[] pixels;
            int srcWidth, srcHeight;
            try
            {
                var bitmapSource = icon.Source as BitmapSource;
                if (bitmapSource == null) return;

                if (bitmapSource.Format != PixelFormats.Bgra32 && bitmapSource.Format != PixelFormats.Pbgra32)
                    bitmapSource = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgra32, null, 0);

                srcWidth = bitmapSource.PixelWidth;
                srcHeight = bitmapSource.PixelHeight;
                int stride = srcWidth * 4;
                pixels = new byte[srcHeight * stride];
                bitmapSource.CopyPixels(pixels, stride, 0);
            }
            catch
            {
                return;
            }

            // Render on background thread
            _renderVersion++;
            int myVersion = _renderVersion;

            Task.Run(() =>
            {
                var glowBitmap = GlowRenderer.RenderGlow(
                    pixels, srcWidth, srcHeight,
                    color1, color2,
                    styleParams, baseSigma,
                    displayWidth, displayHeight,
                    intensity);

                if (glowBitmap == null) return;

                Application.Current?.Dispatcher?.BeginInvoke(
                    DispatcherPriority.Loaded,
                    new Action(() =>
                    {
                        if (myVersion != _renderVersion) return;
                        ApplyGlowToIcon(icon, glowBitmap, styleParams, baseSigma);
                    }));
            });
        }

        private void ApplyGlowToIcon(Image icon, BitmapSource glowBitmap, GlowStyleParams styleParams, double baseSigma)
        {
            double extend = GlowRenderer.CalculateExtend(styleParams, baseSigma);
            var glowImage = GlowRenderer.CreateGlowImage(glowBitmap, icon.ActualWidth, icon.ActualHeight, extend);

            // If same icon is already wrapped, just replace the glow image
            if (_currentIcon == icon && _currentWrapperGrid != null
                && _currentParentPanel != null && _currentParentPanel.Children.Contains(_currentWrapperGrid))
            {
                if (_currentWrapperGrid.Children.Count > 0)
                    _currentWrapperGrid.Children.RemoveAt(0);
                _currentWrapperGrid.Children.Insert(0, glowImage);
                _currentGlowImage = glowImage;
                return;
            }

            // Remove any existing glow first
            RemoveGlow();

            // Find icon's parent panel
            var parent = VisualTreeHelper.GetParent(icon) as Panel;
            if (parent == null) return;

            int iconIndex = parent.Children.IndexOf(icon);
            if (iconIndex < 0) return;

            // Store state for cleanup
            _currentIcon = icon;
            _currentParentPanel = parent;
            _originalIconIndex = iconIndex;

            // Create wrapper grid
            var wrapper = new Grid();
            wrapper.ClipToBounds = false;
            wrapper.Margin = icon.Margin;
            icon.Margin = new Thickness(0);

            // Transfer DockPanel.Dock if parent is a DockPanel
            if (parent is DockPanel)
            {
                DockPanel.SetDock(wrapper, DockPanel.GetDock(icon));
            }

            // Swap: remove icon, insert wrapper with glow + icon
            parent.Children.RemoveAt(iconIndex);
            wrapper.Children.Add(glowImage);
            wrapper.Children.Add(icon);
            parent.Children.Insert(Math.Min(iconIndex, parent.Children.Count), wrapper);

            _currentWrapperGrid = wrapper;
            _currentGlowImage = glowImage;
        }

        public void RemoveGlow()
        {
            bool wrapperIsLive = _currentWrapperGrid != null && _currentParentPanel != null
                              && _currentParentPanel.Children.Contains(_currentWrapperGrid);

            if (wrapperIsLive && _currentIcon != null)
            {
                try
                {
                    _currentIcon.Margin = _currentWrapperGrid.Margin;
                    _currentWrapperGrid.Children.Clear();
                    _currentParentPanel.Children.Remove(_currentWrapperGrid);
                    _currentParentPanel.Children.Insert(
                        Math.Min(_originalIconIndex, _currentParentPanel.Children.Count),
                        _currentIcon);
                }
                catch
                {
                    // Visual tree may have been rebuilt by Playnite
                }
            }

            _currentGlowImage = null;
            _currentWrapperGrid = null;
            _currentIcon = null;
            _currentParentPanel = null;
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
