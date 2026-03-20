# Changelog

## 1.1.0 (2026-03-19)

### Icon Effects — Shimmer & Luster
- **Shimmer** effect: animated diagonal shine bar sweeps across the icon with configurable speed, pause timing, and opacity
- **Color Shimmer**: shimmer using the icon's extracted colors instead of white
- **Shine Sweep**: standalone WPF-based shine bar or orbiting ellipse with independent speed and pause controls
- **Metallic Luster**: per-pixel directional lighting applied after the shimmer bar — brightens and saturates the lit side, dims and desaturates the shadow side. Responds dynamically to the shimmer tilt cycle
- **Tilt**: subtle skew transform synchronized with shimmer cycle
- **Hover Effect**: mouse-reactive tilt, scale, and levitation
- **Breathing Scale**: slow continuous scale pulse

### Shine Styles with Style-Specific Luster
- 6 shimmer shine styles: **White**, **Gold**, **Holographic**, **Platinum**, **Crimson**, **Icon Colors**
- Each shine style tints the luster effect to match — Gold gives warm golden highlights with bronze shadows, Platinum gives cool chrome, Crimson gives red-hot ember glow, Holographic shifts rainbow across the surface

### Effect Shape
- **Square** mode: directional sweep effects for standard square icons
- **Circular** mode: orbiting highlight effects for circular or transparent icon shapes
- Radial shimmer spot, orbiting shine sweep ellipse, and radial luster with continuous orbit angle

### Transform Effects
- **Levitation**: slow continuous sine-wave float up and down, configurable speed (2–10s) and amplitude (0.5–6px)
- **3D Rotation**: fake perspective turntable using SkewTransform.AngleY + ScaleX squash, configurable speed (3–15s) and amount (1–8°)
- **Shadow Drift**: glow image margin shifts opposite to tilt direction for depth illusion
- **Parallax**: glow layer shifts horizontally opposite to tilt for depth between glow and icon

### Theme Presets
- 13 preset configurations across 5 categories: Glow Only, Metallic, Ambient, Signature, Showcase
- Each preset calls `ResetToCleanBaseline()` first to prevent leftover state from previous presets
- **Huddini - Synthwave Cruiser** in Square and Circle variants
- Presets now use levitation, 3D rotation, shadow drift, and parallax where thematically appropriate

### About Tab
- New settings tab explaining how BeautyCons works (visual tree injection) and known WPF limitations
- Credits section

### Circular Effect Improvements
- Tighter orbit radii and gradient sizes to stay within circular icon bounds
- Circular `ClipPath` applied to shimmer spot and luster to prevent gradient overflow
- Shine sweep ellipse reduced and orbit tightened for circular icons

### Technical
- SkiaSharp `canvas.Flush()` before per-pixel luster reads for reliable post-shimmer modification
- Continuous orbit angle accumulation prevents radial animation jumps at cycle boundaries
- Screen-blend brightness with saturation boost for natural color-preserving luster
- Style-specific luster tinting: each ShineStyle defines highlight and shadow color pairs
- Luminance-weighted masking with box-blurred luminance map for content-aware effects

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
