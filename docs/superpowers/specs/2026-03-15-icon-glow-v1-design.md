# BeautyCons v1.0.0 — Static Icon Glow Effects

## Purpose

BeautyCons is a Playnite extension that applies beautiful, customizable glow effects around game icons in the detail view. This first version focuses on static (non-animated) glow effects for the currently selected game.

## Decisions

| Decision | Choice |
|----------|--------|
| Scope | Static glow effects, selected game only, detail view |
| Styles | Neon, Soft, Sharp — parameterized via `GlowStyle` |
| Rendering | Pure SkiaSharp on background thread, frozen `BitmapSource` output |
| Colors | Auto-extracted from icon (2 dominant hues), optional custom hex override |
| Default state | Enabled on install, Neon style |
| Settings | Enable, style, size, intensity, custom colors with hex input |
| Logging | None for v1 (silent fallbacks on all error paths) |
| Icon discovery | Visual tree traversal targeting `PART_ImageIcon` |
| Icon wrapping | Grid wrapper with glow image behind icon |
| Dependencies | SkiaSharp 2.88.9 + SkiaSharp.NativeAssets.Win32 2.88.9 |

## Architecture

### File Structure

```
src/
  IconGlow/
    GlowRenderer.cs        — SkiaSharp rendering (static class)
    GlowStyle.cs            — Enum + parameter structs + GetParams() factory
    IconColorExtractor.cs   — Hue-bucket color extraction with per-game cache
    TileFinder.cs           — Visual tree traversal to find PART_ImageIcon
    IconGlowManager.cs      — Orchestrator: find icon, extract colors, render, wrap
  BeautyCons.cs             — Plugin entry point (modify existing)
  BeautyConsSettings.cs     — Settings model (modify existing)
  BeautyConsSettingsViewModel.cs — Settings VM (modify existing)
  BeautyConsSettingsView.xaml    — Settings UI (modify existing)
  BeautyConsSettingsView.xaml.cs — Settings codebehind (modify existing)
  BeautyCons.csproj              — Add SkiaSharp dependencies (modify existing)
scripts/
  package_extension.ps1     — Add SkiaSharp native DLL copying (modify existing)
```

### Data Flow

```
Game Selected (GenericPlugin.OnGameSelected override)
  → TileFinder locates PART_ImageIcon in detail view (UI thread)
  → IconColorExtractor extracts 2 dominant colors from icon bitmap (UI thread)
  → Icon pixel data extracted to byte[] on UI thread
  → GlowRenderer renders glow via SkiaSharp (background thread via Task.Run)
  → Frozen BitmapSource marshaled back to UI thread
  → IconGlowManager wraps icon in Grid, places glow Image behind icon
  → Game changes → old glow removed, new glow rendered
```

## Component Details

### GlowRenderer

Static class. Two public methods:

```csharp
public static BitmapSource RenderGlow(
    byte[] srcPixels, int srcWidth, int srcHeight,
    Color color1, Color color2,
    GlowStyleParams styleParams, double baseSigma,
    double displayWidth, double displayHeight,
    double intensity)
```

- `srcPixels`: Bgra32 pixel data, stride = `srcWidth * 4`, no padding. Extracted on UI thread by caller.
- `baseSigma`: The user's `IconGlowSize` setting (2.0–12.0), used as the base blur sigma that all layer `SigmaMultiplier` values are applied against.
- `intensity`: The user's `IconGlowIntensity` setting (0.5–3.0), scales luminance in tinted bitmaps.

**Process:**
1. Use the largest `SigmaMultiplier` across all layers to calculate extend: `extend = ceil(maxSigma * baseSigma * 2.5)`
2. Create SkiaSharp `SKSurface` at `(displayWidth + extend*2) x (displayHeight + extend*2)`
3. Create `SKBitmap` from `srcPixels`
4. Build two tinted bitmaps (one per color): RGB = tint color, Alpha = `min(1.0, luminance * intensity) * sourceAlpha`
5. Calculate color shift offset: `baseSigma * styleParams.ColorShiftFactor` pixels — this spatially offsets the two tinted bitmaps creating a gradient separation effect
6. For each layer in `styleParams.Layers`: draw both tinted bitmaps with `SKImageFilter.CreateBlur(sigma, sigma)` where `sigma = baseSigma * layer.SigmaMultiplier`, using `styleParams.BlendMode` and `layer.Alpha`
7. Encode to PNG, return as frozen `BitmapSource`

```csharp
public static Image CreateGlowImage(
    BitmapSource glowBitmap, double iconWidth, double iconHeight,
    double extendSize)
```

Creates a WPF `Image` element sized to `(iconWidth + extendSize*2) x (iconHeight + extendSize*2)` with `Margin = new Thickness(-extendSize)`, `IsHitTestVisible = false`, `Stretch = Fill`.

**Extend size calculation** (used by both `RenderGlow` and `CreateGlowImage`):

```csharp
public static double CalculateExtend(GlowStyleParams styleParams, double baseSigma)
{
    float maxSigma = 0;
    foreach (var layer in styleParams.Layers)
        maxSigma = Math.Max(maxSigma, layer.SigmaMultiplier);
    return Math.Ceiling(maxSigma * baseSigma * 2.5);
}
```

### GlowStyle

```csharp
public enum GlowStyle { Neon, Soft, Sharp }

public struct GlowStyleParams
{
    public GlowLayer[] Layers;
    public float ColorShiftFactor;  // spatial offset between color1/color2 draw positions, as multiplier of baseSigma
    public SKBlendMode BlendMode;
}

public struct GlowLayer
{
    public float SigmaMultiplier;  // multiplied by baseSigma (IconGlowSize) to get actual blur sigma
    public float Alpha;            // opacity of this layer (0.0–1.0)
}
```

**Static factory method:**

```csharp
public static GlowStyleParams GetParams(GlowStyle style)
```

Returns the parameter set for each style. `IconGlowSize` (the user setting) is the `baseSigma` that all `SigmaMultiplier` values are applied against.

**Neon** — 3 layers, additive blending:
- Layer 1: sigma × 2.0, alpha 0.5 (wide soft outer)
- Layer 2: sigma × 1.0, alpha 0.7 (medium)
- Layer 3: sigma × 0.5, alpha 0.9 (tight bright inner)
- Color shift factor: 0.4
- Blend: `SKBlendMode.Plus`

**Soft** — 1 layer, normal blending:
- Layer 1: sigma × 2.5, alpha 0.6 (wide dreamy)
- Color shift factor: 0.8
- Blend: `SKBlendMode.SrcOver`

**Sharp** — 2 layers, additive blending:
- Layer 1: sigma × 0.4, alpha 0.9 (tight edge)
- Layer 2: sigma × 0.8, alpha 0.3 (subtle outer)
- Color shift factor: 0.2
- Blend: `SKBlendMode.Plus`

### IconColorExtractor

Extracts two dominant vivid colors from a game icon:

1. Convert to Bgra32, scan all pixels
2. Bucket by hue (12 buckets, 30 degrees each)
3. Filter: skip transparent (alpha < 100), low saturation (< 0.15), too dark (< 0.10), near-white (> 0.95)
4. Track best vividness (saturation * value) per bucket
5. Pick top 2 non-adjacent buckets
6. HSV-boost results: min saturation 0.6, min value 0.7
7. If only 1 good bucket, shift hue by 60 degrees for secondary

Cache: `ConcurrentDictionary<Guid, (Color, Color)>` keyed by game ID. Cleared when custom color settings change.

Fallback colors: cornflower blue `#6495ED`, purple `#B464FF`.

### TileFinder

Static utility for Playnite visual tree traversal:

1. Find `PART_ControlGameView` (game detail panel)
2. Within it, find `PART_ImageIcon`
3. Fallback: scan entire tree for any visible `PART_ImageIcon` with non-zero dimensions

Also provides `FindChildByName<T>()` and `FindAncestor<T>()` helpers.

### IconGlowManager

Orchestrator that ties everything together.

**Constructor:**

```csharp
public IconGlowManager(BeautyConsSettingsViewModel settingsViewModel)
```

Takes a reference to the settings ViewModel (not the Settings object directly). Accesses settings via `_settingsViewModel.Settings`. This ensures the manager always reads the current settings object even after `CancelEdit()` replaces it. Subscribes to `_settingsViewModel.Settings.PropertyChanged` for live changes, and re-subscribes whenever `_settingsViewModel.PropertyChanged` fires for the `Settings` property (indicating the settings object was replaced by `CancelEdit` or reload).

**State tracked:**
- `_currentIcon` (`Image`) — the icon element in the visual tree
- `_currentWrapperGrid` (`Grid`) — the wrapper containing glow + icon
- `_currentParentPanel` (`Panel`) — original parent of the icon
- `_originalIconIndex` (`int`) — icon's index in parent before wrapping
- `_renderVersion` (`int`) — incremented on each game select; background render results are discarded if version has changed by the time they complete
- `_activeGame` (`Game`) — the currently selected game

**OnGameSelected(Game game):**
1. If glow disabled, call `RemoveGlow()` and return
2. Store `_activeGame = game`
3. Dispatch to UI thread at `DispatcherPriority.Loaded`
4. Call `ApplyGlow(game)`

**ApplyGlow(Game game):**
1. Find icon via `TileFinder.FindSelectedGameIcon(Application.Current.MainWindow)`
2. If not found, silently return
3. Get colors: if `UseCustomColors`, parse hex strings (fallback to defaults on parse failure); otherwise extract via `IconColorExtractor`
4. Get style params: `GlowStyleParams.GetParams(settings.IconGlowStyle)`
5. Extract icon pixel data on UI thread: convert `icon.Source` to Bgra32 `byte[]` (stride = width * 4, no padding)
6. Capture `baseSigma = settings.IconGlowSize` and `intensity = settings.IconGlowIntensity`
7. Increment `_renderVersion`, capture local copy `myVersion`
8. `Task.Run(() => GlowRenderer.RenderGlow(pixels, width, height, color1, color2, styleParams, baseSigma, displayWidth, displayHeight, intensity))`
8. On completion, marshal to UI thread via `Dispatcher.BeginInvoke`
9. Check `myVersion == _renderVersion` — if stale, discard result
10. Call `ApplyGlowToIcon(icon, glowBitmap, styleParams)`

**ApplyGlowToIcon(icon, glowBitmap, styleParams):**
1. Calculate extend via `GlowRenderer.CalculateExtend(styleParams, settings.IconGlowSize)`
2. Create glow image via `GlowRenderer.CreateGlowImage(glowBitmap, icon.ActualWidth, icon.ActualHeight, extend)`
3. If icon already wrapped (same `_currentIcon` reference and wrapper is live): replace glow image at index 0
4. If new icon: remove icon from parent, create Grid wrapper (`ClipToBounds = false`), add glow at index 0 + icon at index 1, transfer icon's `Margin` to wrapper, conditionally transfer `DockPanel.Dock` only if parent is a `DockPanel`, insert wrapper at original index

**RemoveGlow():**
1. If wrapper is live in visual tree (`_currentParentPanel.Children.Contains(_currentWrapperGrid)`): restore icon margins, unwrap, reinsert icon at original index
2. If wrapper is orphaned (Playnite rebuilt tree): just drop references
3. Clear all state references

**Destroy():**
- Unsubscribe from both `_settingsViewModel.PropertyChanged` and `_settingsViewModel.Settings.PropertyChanged`
- Call `RemoveGlow()`
- Clear color cache

**Settings change handling:**
- `PropertyChanged` handler checks property name against: `EnableIconGlow`, `IconGlowStyle`, `IconGlowSize`, `IconGlowIntensity`, `UseCustomColors`, `CustomColor1`, `CustomColor2`
- If `EnableIconGlow` changed to false: `RemoveGlow()`
- If `EnableIconGlow` changed to true or other glow property changed: check `_currentIcon != null && _currentIcon.IsLoaded` for validity; if valid, re-render with current icon; if stale, re-find icon via `ApplyGlow(_activeGame)`

### BeautyCons.cs (Plugin Entry Point)

```
Constructor:
  - Create IconGlowManager(settingsViewModel)

OnApplicationStarted:
  - Use DispatcherTimer (1 second, single-shot) to defer initial glow:
    - Get PlayniteApi.MainView.SelectedGames
    - If any, call iconGlowManager.OnGameSelected(first game)
    - Stop timer

OnGameSelected (override GenericPlugin.OnGameSelected):
  - If args.NewValue has games, call iconGlowManager.OnGameSelected(first game)
  - If none, call iconGlowManager.OnGameSelected(null) to remove glow

OnApplicationStopped:
  - Call iconGlowManager.Destroy()

GetSettings / GetSettingsView:
  - Same pattern as current (cached ViewModel)
```

### Settings

**New properties on `BeautyConsSettings` (with ObservableObject PropertyChanged):**

```csharp
bool EnableIconGlow = true
GlowStyle IconGlowStyle = GlowStyle.Neon
double IconGlowSize = 6.0        // range 2.0–12.0, this is baseSigma for blur
double IconGlowIntensity = 1.8   // range 0.5–3.0, luminance scaling in tint
bool UseCustomColors = false
string CustomColor1 = "#6495ED"  // stored as hex string for JSON serialization
string CustomColor2 = "#B464FF"
```

All properties use the same `get => field; set { field = value; OnPropertyChanged(); }` pattern already established in the settings class. `IconGlowManager` subscribes to `PropertyChanged` directly.

### BeautyConsSettingsViewModel Changes

- `BeginEdit()`: No change needed (no snapshot required for v1)
- `CancelEdit()`: Reload settings from saved (existing behavior). When the `Settings` property setter fires `OnPropertyChanged()`, `IconGlowManager` detects the object replacement and re-subscribes to the new settings object's `PropertyChanged`, then re-applies the glow with the restored settings.
- `EndEdit()`: Save settings (existing behavior)
- `VerifySettings()`: No change needed — invalid hex colors fall back to defaults at render time

### Settings UI

Add "Icon Glow" tab to the existing `TabControl`:

- "Enable Icon Glow" checkbox → binds to `Settings.EnableIconGlow`
- "Glow Style" ComboBox (Neon / Soft / Sharp) → binds to `Settings.IconGlowStyle`
- "Glow Size" slider (2.0–12.0, step 0.5) → binds to `Settings.IconGlowSize`
- "Glow Intensity" slider (0.5–3.0, step 0.1) → binds to `Settings.IconGlowIntensity`
- "Use Custom Colors" checkbox → binds to `Settings.UseCustomColors`
- "Primary Color" TextBox (hex, e.g. `#6495ED`) → binds to `Settings.CustomColor1`, enabled only when `UseCustomColors` is true
- "Secondary Color" TextBox (hex) → binds to `Settings.CustomColor2`, enabled only when `UseCustomColors` is true
- "Reset to Defaults" button → `ResetIconGlowTab_Click` handler

**ResetIconGlowTab_Click handler** (in codebehind):
- Show confirmation dialog via `PlayniteApi.Dialogs.ShowMessage`
- If confirmed, reset: `EnableIconGlow = true`, `IconGlowStyle = Neon`, `IconGlowSize = 6.0`, `IconGlowIntensity = 1.8`, `UseCustomColors = false`, `CustomColor1 = "#6495ED"`, `CustomColor2 = "#B464FF"`

## Dependencies

**New NuGet packages:**
- `SkiaSharp` 2.88.9
- `SkiaSharp.NativeAssets.Win32` 2.88.9 (provides native `libSkiaSharp.dll` in build output)

**Native DLL loading strategy:**
Playnite runs as x64 on modern Windows. The packaging script copies `libSkiaSharp.dll` (x64) flat to the package root alongside `SkiaSharp.dll`. The existing `AssemblyResolve` handler in `BeautyCons.cs` resolves managed DLLs from the extension directory. For the native DLL, SkiaSharp's P/Invoke will find `libSkiaSharp.dll` via standard Windows DLL search order since it is in the same directory as the managed assembly. No `SetDllDirectory` call is needed.

**Packaging changes (`package_extension.ps1`):**
- Copy `SkiaSharp.dll` (managed) to package root
- Copy `libSkiaSharp.dll` (x64 native) flat to package root (not in subdirectory)
- Expected package size: ~3–4 MB

## Error Handling

All error paths are silent — no logging, no user-facing errors:
- Icon not found in visual tree → skip glow, return
- SkiaSharp render returns null → skip glow, return
- Icon has zero dimensions → skip, return
- Color extraction finds no colorful pixels → use fallback colors
- Custom color hex parse fails → use fallback colors
- Wrapper grid orphaned by Playnite tree rebuild → drop references, re-wrap on next selection
- Stale icon reference on settings change → re-find icon via full ApplyGlow path

## Testing

Manual testing in Playnite Desktop mode:
1. Install extension, verify glow appears on selected game immediately
2. Switch between games, verify glow updates without visual artifacts
3. Toggle each style (Neon/Soft/Sharp), verify distinct visual differences
4. Adjust size and intensity sliders, verify glow updates
5. Enable custom colors, enter hex values, verify glow uses custom colors
6. Disable glow, verify icon returns to normal
7. Re-enable glow, verify it reappears
8. Switch Playnite themes, verify glow still finds and wraps the icon

## Future Extensions (Not in v1)

- Animated glow (pulse, spin, color cycling)
- Audio-reactive intensity (like UniPlaySong)
- List/grid hover glow
- Particle effects
- More glow styles
- Built-in color picker control
