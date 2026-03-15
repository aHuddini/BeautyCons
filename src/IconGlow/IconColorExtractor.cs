using System;
using System.Collections.Concurrent;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BeautyCons.IconGlow
{
    public class IconColorExtractor
    {
        private static readonly Color DefaultColor1 = Color.FromRgb(100, 149, 237);
        private static readonly Color DefaultColor2 = Color.FromRgb(180, 100, 255);

        private readonly ConcurrentDictionary<Guid, (Color primary, Color secondary)> _cache =
            new ConcurrentDictionary<Guid, (Color, Color)>();

        public (Color primary, Color secondary) GetGlowColors(Guid gameId, ImageSource imageSource)
        {
            if (_cache.TryGetValue(gameId, out var cached))
                return cached;

            var colors = ExtractFromBitmap(imageSource as BitmapSource);
            _cache.TryAdd(gameId, colors);
            return colors;
        }

        public void ClearCache() => _cache.Clear();

        private (Color primary, Color secondary) ExtractFromBitmap(BitmapSource bitmap)
        {
            if (bitmap == null)
                return (DefaultColor1, DefaultColor2);

            try
            {
                var formatted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                int width = formatted.PixelWidth;
                int height = formatted.PixelHeight;
                if (width == 0 || height == 0)
                    return (DefaultColor1, DefaultColor2);

                int stride = width * 4;
                byte[] pixels = new byte[stride * height];
                formatted.CopyPixels(pixels, stride, 0);

                const int BucketCount = 12;
                double[] bucketScores = new double[BucketCount];
                byte[] bestR = new byte[BucketCount];
                byte[] bestG = new byte[BucketCount];
                byte[] bestB = new byte[BucketCount];
                double[] bestVividness = new double[BucketCount];
                int totalColorPixels = 0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = y * stride + x * 4;
                        byte b = pixels[offset];
                        byte g = pixels[offset + 1];
                        byte r = pixels[offset + 2];
                        byte a = pixels[offset + 3];

                        if (a < 100) continue;

                        var hsv = RgbToHsv(r, g, b);
                        if (hsv.saturation < 0.15) continue;
                        if (hsv.value < 0.10) continue;
                        if (hsv.value > 0.95) continue;

                        totalColorPixels++;
                        int bucket = ((int)hsv.hue / 30) % BucketCount;
                        double vividness = hsv.saturation * hsv.value;
                        bucketScores[bucket] += vividness;

                        if (vividness > bestVividness[bucket])
                        {
                            bestVividness[bucket] = vividness;
                            bestR[bucket] = r;
                            bestG[bucket] = g;
                            bestB[bucket] = b;
                        }
                    }
                }

                if (totalColorPixels == 0)
                    return (DefaultColor1, DefaultColor2);

                int win1 = -1;
                double score1 = 0;
                for (int i = 0; i < BucketCount; i++)
                {
                    if (bucketScores[i] > score1)
                    {
                        score1 = bucketScores[i];
                        win1 = i;
                    }
                }

                int win2 = -1;
                double score2 = 0;
                for (int i = 0; i < BucketCount; i++)
                {
                    int dist = Math.Abs(i - win1);
                    if (dist > BucketCount / 2) dist = BucketCount - dist;
                    if (dist <= 1) continue;

                    if (bucketScores[i] > score2)
                    {
                        score2 = bucketScores[i];
                        win2 = i;
                    }
                }

                var primary = BoostColor(bestR[win1], bestG[win1], bestB[win1]);

                Color secondary;
                if (win2 >= 0 && score2 > score1 * 0.1)
                {
                    secondary = BoostColor(bestR[win2], bestG[win2], bestB[win2]);
                }
                else
                {
                    var hsv1 = RgbToHsv(primary.R, primary.G, primary.B);
                    double shiftedHue = (hsv1.hue + 60) % 360;
                    var shifted = HsvToRgb(shiftedHue, Math.Max(hsv1.saturation, 0.6), Math.Max(hsv1.value, 0.7));
                    secondary = Color.FromRgb(shifted.r, shifted.g, shifted.b);
                }

                return (primary, secondary);
            }
            catch
            {
                return (DefaultColor1, DefaultColor2);
            }
        }

        private static Color BoostColor(byte r, byte g, byte b)
        {
            var hsv = RgbToHsv(r, g, b);
            double s = Math.Max(hsv.saturation, 0.6);
            double v = Math.Max(hsv.value, 0.7);
            var result = HsvToRgb(hsv.hue, s, v);
            return Color.FromRgb(result.r, result.g, result.b);
        }

        private static (double hue, double saturation, double value) RgbToHsv(byte r, byte g, byte b)
        {
            double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            double hue = 0;
            if (delta > 0)
            {
                if (max == rd) hue = 60 * (((gd - bd) / delta) % 6);
                else if (max == gd) hue = 60 * (((bd - rd) / delta) + 2);
                else hue = 60 * (((rd - gd) / delta) + 4);
            }
            if (hue < 0) hue += 360;

            double saturation = max > 0 ? delta / max : 0;
            return (hue, saturation, max);
        }

        private static (byte r, byte g, byte b) HsvToRgb(double h, double s, double v)
        {
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

            return (
                (byte)Math.Min(255, (r1 + m) * 255),
                (byte)Math.Min(255, (g1 + m) * 255),
                (byte)Math.Min(255, (b1 + m) * 255)
            );
        }
    }
}
