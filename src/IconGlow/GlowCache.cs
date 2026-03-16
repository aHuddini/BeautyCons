using System;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BeautyCons.IconGlow
{
    public class GlowCache
    {
        private readonly ConcurrentDictionary<string, BitmapSource> _memoryCache =
            new ConcurrentDictionary<string, BitmapSource>();
        private readonly string _diskCacheDir;

        public GlowCache(string extensionsDataPath)
        {
            _diskCacheDir = Path.Combine(extensionsDataPath, "BeautyCons", "GlowCache");
        }

        public static string BuildCacheKey(Guid gameId, GlowStyle style, double size, double intensity,
            bool useCustomColors, string customColor1, string customColor2,
            Color extractedColor1, Color extractedColor2)
        {
            string colorPart;
            if (useCustomColors)
            {
                colorPart = $"c_{customColor1}_{customColor2}";
            }
            else
            {
                colorPart = $"e_{extractedColor1.R:X2}{extractedColor1.G:X2}{extractedColor1.B:X2}" +
                            $"_{extractedColor2.R:X2}{extractedColor2.G:X2}{extractedColor2.B:X2}";
            }

            return $"{gameId}_{style}_{size:F1}_{intensity:F1}_{colorPart}";
        }

        public BitmapSource TryGetMemory(string key)
        {
            if (_memoryCache.TryGetValue(key, out var bitmap))
                return bitmap;
            return null;
        }

        public BitmapSource TryLoadFromDisk(string key)
        {
            try
            {
                string filePath = GetDiskPath(key);
                if (!File.Exists(filePath)) return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                // Promote to memory cache
                _memoryCache[key] = bitmap;
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public void Store(string key, BitmapSource bitmap)
        {
            if (bitmap == null) return;

            // Store in memory
            _memoryCache[key] = bitmap;

            // Store to disk on background thread
            try
            {
                if (!Directory.Exists(_diskCacheDir))
                    Directory.CreateDirectory(_diskCacheDir);

                string filePath = GetDiskPath(key);
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fs);
                }
            }
            catch
            {
                // Disk write failure is non-fatal
            }
        }

        public void InvalidateForSettings()
        {
            // Settings changed — clear all caches since keys include style params
            _memoryCache.Clear();
        }

        public void ClearAll()
        {
            _memoryCache.Clear();

            try
            {
                if (Directory.Exists(_diskCacheDir))
                    Directory.Delete(_diskCacheDir, true);
            }
            catch
            {
                // Non-fatal
            }
        }

        private string GetDiskPath(string key)
        {
            // Sanitize key for filesystem
            string safe = key.Replace("#", "").Replace(" ", "");
            foreach (char c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');
            return Path.Combine(_diskCacheDir, safe + ".png");
        }
    }
}
