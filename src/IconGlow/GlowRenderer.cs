using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;

namespace BeautyCons.IconGlow
{
    public static class GlowRenderer
    {
        public static double CalculateExtend(GlowStyleParams styleParams, double baseSigma)
        {
            float maxSigma = 0;
            foreach (var layer in styleParams.Layers)
                maxSigma = Math.Max(maxSigma, layer.SigmaMultiplier);
            return Math.Ceiling(maxSigma * baseSigma * 2.5);
        }

        public static BitmapSource RenderGlow(
            byte[] srcPixels, int srcWidth, int srcHeight,
            Color color1, Color color2,
            GlowStyleParams styleParams, double baseSigma,
            double displayWidth, double displayHeight,
            double intensity)
        {
            try
            {
                if (srcPixels == null || srcWidth <= 0 || srcHeight <= 0)
                    return null;

                int targetW = (int)Math.Ceiling(displayWidth);
                int targetH = (int)Math.Ceiling(displayHeight);
                if (targetW <= 0 || targetH <= 0) return null;

                double extend = CalculateExtend(styleParams, baseSigma);
                int ext = (int)extend;
                int outWidth = targetW + ext * 2;
                int outHeight = targetH + ext * 2;

                float shift = (float)(baseSigma * styleParams.ColorShiftFactor);

                using (var surface = SKSurface.Create(new SKImageInfo(outWidth, outHeight, SKColorType.Bgra8888, SKAlphaType.Premul)))
                {
                    if (surface == null) return null;

                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);

                    var skInfo = new SKImageInfo(srcWidth, srcHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                    using (var skBitmap = new SKBitmap(skInfo))
                    {
                        var ptr = skBitmap.GetPixels();
                        Marshal.Copy(srcPixels, 0, ptr, srcPixels.Length);
                        skBitmap.NotifyPixelsChanged();

                        var tinted1 = BuildTintedBitmap(skBitmap, color1, intensity);
                        var tinted2 = BuildTintedBitmap(skBitmap, color2, intensity);

                        var rect1 = new SKRect(
                            ext - shift, ext - shift,
                            ext + targetW - shift, ext + targetH - shift);
                        var rect2 = new SKRect(
                            ext + shift, ext + shift,
                            ext + targetW + shift, ext + targetH + shift);

                        foreach (var layer in styleParams.Layers)
                        {
                            float sigma = (float)(baseSigma * layer.SigmaMultiplier);
                            DrawBlurredLayer(canvas, tinted1, rect1, sigma, layer.Alpha, styleParams.BlendMode);
                            DrawBlurredLayer(canvas, tinted2, rect2, sigma, layer.Alpha, styleParams.BlendMode);
                        }

                        tinted1.Dispose();
                        tinted2.Dispose();
                    }

                    using (var snapshot = surface.Snapshot())
                    using (var data = snapshot.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        if (data == null) return null;

                        var ms = new MemoryStream();
                        data.SaveTo(ms);
                        ms.Position = 0;

                        var result = new BitmapImage();
                        result.BeginInit();
                        result.CacheOption = BitmapCacheOption.OnLoad;
                        result.StreamSource = ms;
                        result.EndInit();
                        result.Freeze();
                        return result;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static SKBitmap BuildTintedBitmap(SKBitmap srcBitmap, Color tint, double intensity)
        {
            int w = srcBitmap.Width, h = srcBitmap.Height;
            var tintedInfo = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
            var tinted = new SKBitmap(tintedInfo);

            var srcPtr = srcBitmap.GetPixels();
            var dstPtr = tinted.GetPixels();
            int pixelCount = w * h;
            byte[] src = new byte[pixelCount * 4];
            byte[] dst = new byte[pixelCount * 4];
            Marshal.Copy(srcPtr, src, 0, src.Length);

            byte tR = tint.R, tG = tint.G, tB = tint.B;
            for (int i = 0; i < pixelCount; i++)
            {
                int off = i * 4;
                byte sB = src[off], sG = src[off + 1], sR = src[off + 2], sA = src[off + 3];

                double lum = (0.299 * sR + 0.587 * sG + 0.114 * sB) / 255.0;
                double boosted = Math.Min(1.0, lum * intensity);
                byte alpha = (byte)(boosted * sA);

                double af = alpha / 255.0;
                dst[off]     = (byte)(tB * af);
                dst[off + 1] = (byte)(tG * af);
                dst[off + 2] = (byte)(tR * af);
                dst[off + 3] = alpha;
            }

            Marshal.Copy(dst, 0, dstPtr, dst.Length);
            tinted.NotifyPixelsChanged();
            return tinted;
        }

        private static void DrawBlurredLayer(SKCanvas canvas, SKBitmap tinted, SKRect destRect,
            float sigma, float alphaScale, SKBlendMode blendMode)
        {
            using (var blur = SKImageFilter.CreateBlur(sigma, sigma))
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.FilterQuality = SKFilterQuality.High;
                paint.ImageFilter = blur;
                paint.BlendMode = blendMode;
                paint.Color = new SKColor(255, 255, 255, (byte)(alphaScale * 255));
                canvas.DrawBitmap(tinted, destRect, paint);
            }
        }

        public static Image CreateGlowImage(BitmapSource glowBitmap, double iconWidth, double iconHeight, double extendSize)
        {
            return new Image
            {
                Source = glowBitmap,
                Width = iconWidth + extendSize * 2,
                Height = iconHeight + extendSize * 2,
                Stretch = Stretch.Fill,
                IsHitTestVisible = false,
                Opacity = 1.0,
                Margin = new Thickness(-extendSize),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }
}
