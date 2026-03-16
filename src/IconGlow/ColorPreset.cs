using System.Windows.Media;

namespace BeautyCons.IconGlow
{
    public enum ColorPreset
    {
        Auto,
        HotPink,
        Ocean,
        Sunset,
        NeonGreen,
        PurpleHaze,
        Ice,
        Fire,
        Gold,
        Vampire,
        Mint,
        Synthwave,
        Monochrome,
        Custom
    }

    public static class ColorPresets
    {
        public static (Color primary, Color secondary) GetColors(ColorPreset preset)
        {
            switch (preset)
            {
                case ColorPreset.HotPink:
                    return (Color.FromRgb(0xFF, 0x69, 0xB4), Color.FromRgb(0xFF, 0x14, 0x93));
                case ColorPreset.Ocean:
                    return (Color.FromRgb(0x00, 0xCE, 0xD1), Color.FromRgb(0x1E, 0x90, 0xFF));
                case ColorPreset.Sunset:
                    return (Color.FromRgb(0xFF, 0x63, 0x47), Color.FromRgb(0xFF, 0xD7, 0x00));
                case ColorPreset.NeonGreen:
                    return (Color.FromRgb(0x39, 0xFF, 0x14), Color.FromRgb(0x00, 0xFF, 0x7F));
                case ColorPreset.PurpleHaze:
                    return (Color.FromRgb(0x8A, 0x2B, 0xE2), Color.FromRgb(0xDA, 0x70, 0xD6));
                case ColorPreset.Ice:
                    return (Color.FromRgb(0xE0, 0xFF, 0xFF), Color.FromRgb(0x87, 0xCE, 0xEB));
                case ColorPreset.Fire:
                    return (Color.FromRgb(0xFF, 0x45, 0x00), Color.FromRgb(0xFF, 0x8C, 0x00));
                case ColorPreset.Gold:
                    return (Color.FromRgb(0xFF, 0xD7, 0x00), Color.FromRgb(0xDA, 0xA5, 0x20));
                case ColorPreset.Vampire:
                    return (Color.FromRgb(0x8B, 0x00, 0x00), Color.FromRgb(0xDC, 0x14, 0x3C));
                case ColorPreset.Mint:
                    return (Color.FromRgb(0x98, 0xFB, 0x98), Color.FromRgb(0x00, 0xFA, 0x9A));
                case ColorPreset.Synthwave:
                    return (Color.FromRgb(0xFF, 0x00, 0xFF), Color.FromRgb(0x00, 0xFF, 0xFF));
                case ColorPreset.Monochrome:
                    return (Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0xC0, 0xC0, 0xC0));
                default:
                    return (Color.FromRgb(100, 149, 237), Color.FromRgb(180, 100, 255));
            }
        }
    }
}
