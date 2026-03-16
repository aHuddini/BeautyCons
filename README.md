# BeautyCons

A Playnite extension that adds beautiful, customizable glow effects around game icons in the detail view.

## Features

### Glow Styles

| Style | Description |
|-------|-------------|
| **Neon** | Bold multi-layer additive glow with color gradient separation |
| **Soft** | Wide dreamy single-layer glow |
| **Sharp** | Tight bright edge highlight |
| **Bloom** | Overexposed look — bright center with wide soft falloff |
| **Halo** | Soft ring around the icon |
| **Diamond** | Diamond-shaped glow emanating from the icon |
| **Cross** | Cross/plus-shaped glow |
| **Star** | 4-pointed star burst |

### Color Presets

Choose from 13 built-in color presets, or let BeautyCons automatically extract colors from each game's icon.

**Built-in presets:** Hot Pink, Ocean, Sunset, Neon Green, Purple Haze, Ice, Fire, Gold, Vampire, Mint, Synthwave, Monochrome

**Auto mode:** Analyzes the game icon's pixels and picks the two most vivid, contrasting colors for a glow that matches each game.

**Custom mode:** Define your own colors with hex values.

### Animations

All animations are optional and can be combined:

- **Spin Rotation** — Glow rotates smoothly around the icon
- **Pulse** — Opacity breathes in and out on a sine wave
- **Color Cycle** — Hue shifts gradually over time
- **Sparkles** — Small bright dots appear, drift, and fade around the glow

### Performance

- Glow rendering happens on a background thread via SkiaSharp — never blocks the UI
- Two-tier cache (memory + disk) means revisited games display their glow instantly
- Smooth transitions between games with phase-aligned animation restart

## Installation

1. Download the latest `.pext` file from [Releases](https://github.com/aHuddini/BeautyCons/releases)
2. Open Playnite
3. Go to **Add-ons → Extensions**
4. Click **Install from file** and select the `.pext` file
5. Restart Playnite

## Settings

Open Playnite Settings → BeautyCons → **Icon Glow** tab.

All settings take effect immediately — no restart required.

## Requirements

- Playnite 10+ (SDK 6.15.0)
- Desktop mode only (not Fullscreen mode)
- Windows x64

## Building from Source

```bash
dotnet restore src/BeautyCons.csproj
dotnet build src/BeautyCons.csproj -c Release
```

Package for distribution:
```powershell
.\scripts\package_extension.ps1 -Configuration Release
```

Output: `pext/BeautyCons.eb7017af-c0ee-4416-aec5-27d516530af7_1_0_0.pext`

## License

[MIT](LICENSE)
