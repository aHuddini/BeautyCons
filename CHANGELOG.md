# Changelog

## 1.0.0 (2026-03-16)

Initial release.

### Glow Effects
- 8 glow styles: **Neon**, **Soft**, **Sharp**, **Bloom**, **Halo**, **Diamond**, **Cross**, **Star**
- Neon: Multi-layer additive glow with color gradient separation
- Soft: Single wide dreamy glow with normal blending
- Sharp: Tight bright edge highlight
- Bloom: Overexposed photography look — bright center, wide soft falloff
- Halo: Soft ring glow with less inner fill
- Diamond, Cross, Star: Geometric shape-based glows rendered as SkiaSharp paths
- Configurable glow size (2.0–12.0) and intensity (0.5–3.0)

### Colors
- 13 built-in color presets: Hot Pink, Ocean, Sunset, Neon Green, Purple Haze, Ice, Fire, Gold, Vampire, Mint, Synthwave, Monochrome
- Auto mode: Extracts two dominant vivid colors from the game icon using hue-bucket analysis
- Custom mode: User-defined hex color values

### Animations
- **Spin rotation**: Glow rotates around the icon. Configurable speed (5–60 seconds per revolution)
- **Pulse**: Sine wave opacity breathing. Configurable speed (1–10 seconds) and minimum opacity (0–80%)
- **Color cycle**: Gradual hue rotation over time. Configurable cycle duration (4–30 seconds)
- **Sparkles**: Particle dots spawn, drift, and fade around the glow. Configurable count (4–30) and speed

### Performance
- SkiaSharp rendering on background thread — never blocks UI
- Two-tier glow cache (in-memory + disk) for instant display on revisited games
- Smooth game-switch transitions: darken from current opacity, fade in new glow, phase-aligned animation restart
- Unified 60fps animation timer for all effects

### Technical
- Targets .NET Framework 4.6.2 with WPF
- Playnite SDK 6.15.0
- SkiaSharp 2.88.9 for high-quality gaussian blur rendering
- Visual tree integration via PART_ImageIcon discovery and Grid wrapper injection
