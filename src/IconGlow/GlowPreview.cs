using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BeautyCons.IconGlow
{
    public class GlowPreview
    {
        private readonly BeautyConsSettings _settings;

        private static readonly (string resourcePath, string label)[] DemoIcons = new[]
        {
            ("PreviewIcons/dark_devotion.png", "Circular Icon (beveled)"),
            ("PreviewIcons/doom2.png", "Square Icon"),
            ("PreviewIcons/duke3d.png", "Circular Icon (transparent)"),
            ("PreviewIcons/deus_ex_iw.png", "Circular Icon (small)"),
            ("PreviewIcons/star_renegades.png", "Square Icon (vibrant)")
        };

        private static readonly ColorPreset[] PreviewPresets = new[]
        {
            ColorPreset.Auto,
            ColorPreset.HotPink,
            ColorPreset.Ocean,
            ColorPreset.Sunset,
            ColorPreset.NeonGreen,
            ColorPreset.PurpleHaze,
            ColorPreset.Ice,
            ColorPreset.Fire,
            ColorPreset.Gold,
            ColorPreset.Vampire,
            ColorPreset.Mint,
            ColorPreset.Synthwave,
            ColorPreset.Retrowave,
            ColorPreset.Vaporwave,
            ColorPreset.Cyberpunk,
            ColorPreset.Aurora,
            ColorPreset.Electric,
            ColorPreset.Plasma,
            ColorPreset.Toxic,
            ColorPreset.Hologram,
            ColorPreset.Dreamscape,
            ColorPreset.Monochrome
        };

        public GlowPreview(BeautyConsSettings settings)
        {
            _settings = settings;
        }

        public void RenderPreview(Panel container)
        {
            container.Children.Clear();

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var mainStack = new StackPanel { Margin = new Thickness(10) };
            scrollViewer.Content = mainStack;
            container.Children.Add(scrollViewer);

            // Settings header
            mainStack.Children.Add(new TextBlock
            {
                Text = $"Style: {_settings.IconGlowStyle}  |  Size: {_settings.IconGlowSize:F1}  |  Intensity: {_settings.IconGlowIntensity:F1}",
                Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0xAA)),
                FontSize = 11,
                Margin = new Thickness(5, 5, 5, 10)
            });

            foreach (var (resourcePath, label) in DemoIcons)
            {
                BitmapSource iconBitmap;
                byte[] pixels;
                int srcW, srcH;
                try
                {
                    var uri = new Uri($"pack://application:,,,/BeautyCons;component/{resourcePath}", UriKind.Absolute);
                    var bmp = new BitmapImage(uri);
                    bmp.Freeze();
                    iconBitmap = bmp;

                    var formatted = iconBitmap;
                    if (formatted.Format != PixelFormats.Bgra32 && formatted.Format != PixelFormats.Pbgra32)
                        formatted = new FormatConvertedBitmap(iconBitmap, PixelFormats.Bgra32, null, 0);

                    srcW = formatted.PixelWidth;
                    srcH = formatted.PixelHeight;
                    int stride = srcW * 4;
                    pixels = new byte[srcH * stride];
                    formatted.CopyPixels(pixels, stride, 0);
                }
                catch { continue; }

                // Game label
                mainStack.Children.Add(new TextBlock
                {
                    Text = label,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(5, 10, 5, 3)
                });

                // Row of all color presets for this icon
                var presetRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 5) };
                var colorExtractor = new IconColorExtractor();

                foreach (var preset in PreviewPresets)
                {
                    Color c1, c2;
                    if (preset == ColorPreset.Auto)
                    {
                        var extracted = colorExtractor.GetGlowColors(Guid.NewGuid(), iconBitmap);
                        c1 = extracted.primary;
                        c2 = extracted.secondary;
                    }
                    else
                    {
                        var presetColors = ColorPresets.GetColors(preset);
                        c1 = presetColors.primary;
                        c2 = presetColors.secondary;
                    }

                    var item = CreatePresetItem(iconBitmap, pixels, srcW, srcH, preset, c1, c2);
                    presetRow.Children.Add(item);
                }

                mainStack.Children.Add(presetRow);
            }
        }

        private FrameworkElement CreatePresetItem(BitmapSource iconBitmap, byte[] pixels, int srcW, int srcH,
            ColorPreset preset, Color c1, Color c2)
        {
            var container = new StackPanel
            {
                Margin = new Thickness(4, 2, 4, 2),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            int iconSize = 40;
            var iconGrid = new Grid
            {
                ClipToBounds = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = iconSize,
                Height = iconSize,
                Margin = new Thickness(18)
            };

            var iconImage = new Image
            {
                Source = iconBitmap,
                Width = iconSize,
                Height = iconSize,
                Stretch = Stretch.Uniform
            };

            var styleParams = GlowStyleParams.GetParams(_settings.IconGlowStyle);
            double baseSigma = _settings.IconGlowSize;
            double intensity = _settings.IconGlowIntensity;

            var sp = styleParams; var bs = baseSigma; var it = intensity;
            var lc1 = c1; var lc2 = c2;
            var p = pixels; var w = srcW; var h = srcH;
            var sz = iconSize;

            Task.Run(() =>
            {
                var glowBitmap = GlowRenderer.RenderGlow(p, w, h, lc1, lc2, sp, bs, sz, sz, it);
                if (glowBitmap == null) return;

                Application.Current?.Dispatcher?.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() =>
                    {
                        double extend = GlowRenderer.CalculateExtend(sp, bs);
                        var glowImage = GlowRenderer.CreateGlowImage(glowBitmap, sz, sz, extend);
                        iconGrid.Children.Insert(0, glowImage);
                    }));
            });

            iconGrid.Children.Add(iconImage);
            container.Children.Add(iconGrid);

            bool isCurrent = preset == _settings.ColorPreset;
            container.Children.Add(new TextBlock
            {
                Text = preset.ToString(),
                Foreground = new SolidColorBrush(isCurrent
                    ? Color.FromRgb(0x66, 0xBB, 0xFF)
                    : Color.FromRgb(0x77, 0x77, 0x77)),
                FontSize = 8,
                FontWeight = isCurrent ? FontWeights.Bold : FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            return container;
        }
    }
}
