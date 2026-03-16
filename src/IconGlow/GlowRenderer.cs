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

                    if (styleParams.Shape != GlowShape.Normal)
                    {
                        // Shape glow: draw the shape itself as the glow source, then blur it
                        DrawShapeGlow(canvas, styleParams, color1, color2, intensity,
                            outWidth, outHeight, ext, targetW, targetH, (float)baseSigma, shift);
                    }
                    else
                    {
                        // Normal glow: use icon bitmap as glow source
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

        private static void DrawShapeGlow(SKCanvas canvas, GlowStyleParams styleParams,
            Color color1, Color color2, double intensity,
            int outWidth, int outHeight, int ext, int targetW, int targetH,
            float baseSigma, float shift)
        {
            // Center of the icon area within the extended surface
            float cx = outWidth / 2f;
            float cy = outHeight / 2f;
            // Shape size based on icon dimensions (slightly larger to wrap around icon)
            float hw = targetW / 2f * 1.1f;
            float hh = targetH / 2f * 1.1f;

            var path1 = CreateShapePath(styleParams.Shape, cx - shift, cy - shift, hw, hh);
            var path2 = CreateShapePath(styleParams.Shape, cx + shift, cy + shift, hw, hh);
            if (path1 == null || path2 == null)
            {
                path1?.Dispose();
                path2?.Dispose();
                return;
            }

            byte r1 = color1.R, g1 = color1.G, b1 = color1.B;
            byte r2 = color2.R, g2 = color2.G, b2 = color2.B;
            byte alpha = (byte)Math.Min(255, 200 * intensity);

            foreach (var layer in styleParams.Layers)
            {
                float sigma = baseSigma * layer.SigmaMultiplier;

                using (var blur = SKImageFilter.CreateBlur(sigma, sigma))
                using (var paint = new SKPaint())
                {
                    paint.IsAntialias = true;
                    paint.ImageFilter = blur;
                    paint.BlendMode = styleParams.BlendMode;
                    paint.Style = SKPaintStyle.Fill;

                    // Draw shape with color1
                    paint.Color = new SKColor(r1, g1, b1, (byte)(layer.Alpha * alpha));
                    canvas.DrawPath(path1, paint);

                    // Draw shape with color2
                    paint.Color = new SKColor(r2, g2, b2, (byte)(layer.Alpha * alpha));
                    canvas.DrawPath(path2, paint);
                }
            }

            path1.Dispose();
            path2.Dispose();
        }

        private static SKPath CreateShapePath(GlowShape shape, float cx, float cy, float hw, float hh)
        {
            var path = new SKPath();

            switch (shape)
            {
                case GlowShape.Diamond:
                    path.MoveTo(cx, cy - hh);       // top
                    path.LineTo(cx + hw, cy);        // right
                    path.LineTo(cx, cy + hh);        // bottom
                    path.LineTo(cx - hw, cy);        // left
                    path.Close();
                    break;

                case GlowShape.Star:
                    // 4-pointed star with thin spikes
                    float innerR = Math.Min(hw, hh) * 0.3f;
                    path.MoveTo(cx, cy - hh);          // top spike
                    path.LineTo(cx + innerR, cy - innerR);
                    path.LineTo(cx + hw, cy);          // right spike
                    path.LineTo(cx + innerR, cy + innerR);
                    path.LineTo(cx, cy + hh);          // bottom spike
                    path.LineTo(cx - innerR, cy + innerR);
                    path.LineTo(cx - hw, cy);          // left spike
                    path.LineTo(cx - innerR, cy - innerR);
                    path.Close();
                    break;

                case GlowShape.Cross:
                    float armW = hw * 0.35f;
                    float armH = hh * 0.35f;
                    path.MoveTo(cx - armW, cy - hh);
                    path.LineTo(cx + armW, cy - hh);
                    path.LineTo(cx + armW, cy - armH);
                    path.LineTo(cx + hw, cy - armH);
                    path.LineTo(cx + hw, cy + armH);
                    path.LineTo(cx + armW, cy + armH);
                    path.LineTo(cx + armW, cy + hh);
                    path.LineTo(cx - armW, cy + hh);
                    path.LineTo(cx - armW, cy + armH);
                    path.LineTo(cx - hw, cy + armH);
                    path.LineTo(cx - hw, cy - armH);
                    path.LineTo(cx - armW, cy - armH);
                    path.Close();
                    break;

                default:
                    path.Dispose();
                    return null;
            }

            return path;
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
