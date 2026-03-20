using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BeautyCons.IconGlow;

namespace BeautyCons
{
    public partial class BeautyConsSettingsView : UserControl
    {
        private readonly BeautyCons _plugin;
        private BeautyConsSettings _subscribedSettings;

        public BeautyConsSettingsView(BeautyCons plugin)
        {
            _plugin = plugin;
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SubscribeToSettings();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeFromSettings();
        }

        private void SubscribeToSettings()
        {
            UnsubscribeFromSettings();

            var vm = DataContext as BeautyConsSettingsViewModel;
            if (vm?.Settings == null) return;

            _subscribedSettings = vm.Settings;
            _subscribedSettings.PropertyChanged += OnSettingsPropertyChanged;
            UpdateCustomColorVisibility(_subscribedSettings.ColorPreset);
        }

        private void UnsubscribeFromSettings()
        {
            if (_subscribedSettings != null)
            {
                _subscribedSettings.PropertyChanged -= OnSettingsPropertyChanged;
                _subscribedSettings = null;
            }
        }

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BeautyConsSettings.ColorPreset))
            {
                if (_subscribedSettings != null)
                    UpdateCustomColorVisibility(_subscribedSettings.ColorPreset);
            }
        }

        private void UpdateCustomColorVisibility(ColorPreset preset)
        {
            if (CustomColorPanel != null && IsLoaded)
                CustomColorPanel.Visibility = preset == ColorPreset.Custom
                    ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            LoadPreview();
        }

        private void LoadPreview()
        {
            var vm = DataContext as BeautyConsSettingsViewModel;
            if (vm == null) return;

            try
            {
                var preview = new GlowPreview(vm.Settings);
                preview.RenderPreview(PreviewContainer);
            }
            catch { }
        }

        private void ResetGeneralTab_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as BeautyConsSettingsViewModel;
            if (vm == null) return;

            var result = vm.PlayniteApi.Dialogs.ShowMessage(
                "Reset General settings to defaults?",
                "Reset General",
                System.Windows.MessageBoxButton.YesNo);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            vm.Settings.EnablePlugin = true;
            vm.Settings.EnableDebugLogging = false;
        }

        private void ResetIconGlowTab_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as BeautyConsSettingsViewModel;
            if (vm == null) return;

            var result = vm.PlayniteApi.Dialogs.ShowMessage(
                "Reset Icon Glow settings to defaults?",
                "Reset Icon Glow",
                System.Windows.MessageBoxButton.YesNo);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            vm.Settings.EnableIconGlow = true;
            vm.Settings.IconGlowStyle = GlowStyle.Neon;
            vm.Settings.IconGlowSize = 3.5;
            vm.Settings.IconGlowIntensity = 1.8;
            vm.Settings.EnableIconGlowSpin = false;
            vm.Settings.IconGlowSpinSpeed = 20.0;
            vm.Settings.EffectShape = EffectShape.Square;
            vm.Settings.EnableShineSweep = false;
            vm.Settings.ShineSweepSpeed = 3.0;
            vm.Settings.ShineSweepPauseMin = 1.0;
            vm.Settings.ShineSweepPauseMax = 4.0;
            vm.Settings.EnableTilt = false;
            vm.Settings.EnableShimmer = false;
            vm.Settings.ShimmerSpeed = 1.5;
            vm.Settings.ShimmerOpacity = 0.85;
            vm.Settings.ShimmerPauseMin = 1.0;
            vm.Settings.ShimmerPauseMax = 4.0;
            vm.Settings.ShimmerShineStyle = ShineStyle.White;
            vm.Settings.EnableColorShimmer = false;
            vm.Settings.ColorShimmerSpeed = 1.5;
            vm.Settings.ColorShimmerOpacity = 0.65;
            vm.Settings.EnablePulse = false;
            vm.Settings.PulseSpeed = 3.0;
            vm.Settings.PulseMinOpacity = 0.4;
            vm.Settings.EnableColorCycle = false;
            vm.Settings.ColorCycleSpeed = 8.0;
            vm.Settings.EnableSparkles = false;
            vm.Settings.SparkleCount = 12;
            vm.Settings.SparkleSpeed = 1.0;
            vm.Settings.ColorPreset = ColorPreset.Auto;
            vm.Settings.CustomColor1 = "#6495ED";
            vm.Settings.CustomColor2 = "#B464FF";
            vm.Settings.EnableHoverEffect = false;
            vm.Settings.EnableBreathingScale = false;
            vm.Settings.EnableLevitation = false;
            vm.Settings.LevitationSpeed = 4.0;
            vm.Settings.LevitationAmount = 2.5;
            vm.Settings.Enable3DRotation = false;
            vm.Settings.RotationSpeed = 6.0;
            vm.Settings.RotationAmount = 3.0;
            vm.Settings.EnableGlint = false;
            vm.Settings.GlintIntervalMin = 4.0;
            vm.Settings.GlintIntervalMax = 10.0;
            vm.Settings.EnableShadowDrift = false;
            vm.Settings.EnableParallax = false;
        }

        // ----------------------------------------------------------------
        //  THEME PRESETS
        // ----------------------------------------------------------------

        private BeautyConsSettings GetSettingsOrNull()
        {
            var vm = DataContext as BeautyConsSettingsViewModel;
            return vm?.Settings;
        }

        /// <summary>
        /// Resets all effect properties to a clean off state before applying a preset.
        /// Prevents leftover values from a previous preset or user config bleeding through.
        /// </summary>
        private void ResetToCleanBaseline(BeautyConsSettings s)
        {
            // Glow
            s.EnableIconGlow = true;
            s.IconGlowStyle = GlowStyle.Neon;
            s.IconGlowSize = 3.5;
            s.IconGlowIntensity = 1.8;
            s.EnableIconGlowSpin = false;
            s.IconGlowSpinSpeed = 20.0;

            // Colors
            s.ColorPreset = ColorPreset.Auto;
            s.CustomColor1 = "#6495ED";
            s.CustomColor2 = "#B464FF";

            // Effect shape
            s.EffectShape = EffectShape.Square;

            // Shimmer
            s.EnableShimmer = false;
            s.ShimmerSpeed = 1.5;
            s.ShimmerOpacity = 0.85;
            s.ShimmerShineStyle = ShineStyle.White;
            s.ShimmerPauseMin = 1.0;
            s.ShimmerPauseMax = 4.0;
            s.EnableTilt = false;

            // Color shimmer
            s.EnableColorShimmer = false;
            s.ColorShimmerSpeed = 1.5;
            s.ColorShimmerOpacity = 0.65;

            // Shine sweep
            s.EnableShineSweep = false;
            s.ShineSweepSpeed = 3.0;
            s.ShineSweepPauseMin = 1.0;
            s.ShineSweepPauseMax = 4.0;

            // Pulse
            s.EnablePulse = false;
            s.PulseSpeed = 3.0;
            s.PulseMinOpacity = 0.4;

            // Color cycle
            s.EnableColorCycle = false;
            s.ColorCycleSpeed = 8.0;

            // Sparkles
            s.EnableSparkles = false;
            s.SparkleCount = 12;
            s.SparkleSpeed = 1.0;

            // Hover / breathing
            s.EnableHoverEffect = false;
            s.EnableBreathingScale = false;

            // Levitation
            s.EnableLevitation = false;
            s.LevitationSpeed = 4.0;
            s.LevitationAmount = 2.5;

            // 3D rotation
            s.Enable3DRotation = false;
            s.RotationSpeed = 6.0;
            s.RotationAmount = 3.0;

            // Glint
            s.EnableGlint = false;
            s.GlintIntervalMin = 4.0;
            // s.GlintIntervalMax = 10.0;

            // Shadow drift / parallax
            s.EnableShadowDrift = false;
            s.EnableParallax = false;
        }

        private void PresetGoldenFoil_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Bloom;
            s.IconGlowSize = 4.0;
            s.IconGlowIntensity = 2.0;
            s.ColorPreset = ColorPreset.Gold;
            s.EnableShimmer = true;
            s.ShimmerSpeed = 1.8;
            s.ShimmerOpacity = 0.90;
            s.ShimmerShineStyle = ShineStyle.Gold;
            s.ShimmerPauseMin = 1.5;
            s.ShimmerPauseMax = 3.5;
            s.EnableTilt = true;
            s.EnableSparkles = true;
            s.SparkleCount = 8;
            s.SparkleSpeed = 0.8;
            // s.EnableGlint = true; // disabled: effect needs redesign
            // s.GlintInterval = 5.0;
            // s.GlintIntervalMax = 12.0;
            s.EnableShadowDrift = true;
        }

        private void PresetChrome_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Sharp;
            s.IconGlowSize = 3.0;
            s.IconGlowIntensity = 1.6;
            s.ColorPreset = ColorPreset.Ice;
            s.EnableShimmer = true;
            s.ShimmerSpeed = 1.3;
            s.ShimmerOpacity = 0.85;
            s.ShimmerShineStyle = ShineStyle.Platinum;
            s.ShimmerPauseMin = 1.0;
            s.ShimmerPauseMax = 3.0;
            s.EnableTilt = true;
            s.Enable3DRotation = true;
            s.RotationSpeed = 8.0;
            s.RotationAmount = 2.5;
            // s.EnableGlint = true; // disabled: effect needs redesign
            // s.GlintInterval = 3.0;
            // s.GlintIntervalMax = 8.0;
        }

        private void PresetEmberForge_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Neon;
            s.IconGlowSize = 4.5;
            s.IconGlowIntensity = 2.2;
            s.ColorPreset = ColorPreset.Fire;
            s.EnableShimmer = true;
            s.ShimmerSpeed = 2.0;
            s.ShimmerOpacity = 0.90;
            s.ShimmerShineStyle = ShineStyle.Crimson;
            s.ShimmerPauseMin = 1.0;
            s.ShimmerPauseMax = 3.0;
            s.EnableTilt = true;
            s.EnableSparkles = true;
            s.SparkleCount = 15;
            s.SparkleSpeed = 1.2;
            s.EnablePulse = true;
            s.PulseSpeed = 4.0;
            s.PulseMinOpacity = 0.5;
            // s.EnableGlint = true; // disabled: effect needs redesign
            // s.GlintInterval = 2.0;
            // s.GlintIntervalMax = 6.0;
            s.EnableParallax = true;
        }

        private void PresetHolographic_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Soft;
            s.IconGlowSize = 4.0;
            s.IconGlowIntensity = 1.8;
            s.EnableShimmer = true;
            s.ShimmerSpeed = 1.5;
            s.ShimmerOpacity = 0.85;
            s.ShimmerShineStyle = ShineStyle.Holographic;
            s.ShimmerPauseMin = 0.5;
            s.ShimmerPauseMax = 2.0;
            s.EnableTilt = true;
            s.EnableColorCycle = true;
            s.ColorCycleSpeed = 10.0;
            s.EnableSparkles = true;
            s.SparkleCount = 10;
            s.SparkleSpeed = 0.7;
            s.Enable3DRotation = true;
            s.RotationSpeed = 7.0;
            s.RotationAmount = 3.0;
            s.EnableParallax = true;
        }

        private void PresetSubtleGleam_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Soft;
            s.IconGlowSize = 3.0;
            s.IconGlowIntensity = 1.2;
            s.EnableShimmer = true;
            s.ShimmerSpeed = 2.5;
            s.ShimmerOpacity = 0.60;
            s.ShimmerPauseMin = 3.0;
            s.ShimmerPauseMax = 6.0;
            s.EnableTilt = true;
            s.EnableLevitation = true;
            s.LevitationSpeed = 6.0;
            s.LevitationAmount = 1.5;
        }

        private void PresetNeonPulse_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Neon;
            s.IconGlowSize = 5.0;
            s.IconGlowIntensity = 2.5;
            s.ColorPreset = ColorPreset.Synthwave;
            s.EnableColorShimmer = true;
            s.ColorShimmerSpeed = 1.2;
            s.ColorShimmerOpacity = 0.75;
            s.ShimmerPauseMin = 1.0;
            s.ShimmerPauseMax = 2.5;
            s.ShimmerShineStyle = ShineStyle.IconColors;
            s.EnableTilt = true;
            s.EnablePulse = true;
            s.PulseSpeed = 3.0;
            s.PulseMinOpacity = 0.3;
            s.EnableColorCycle = true;
            s.ColorCycleSpeed = 6.0;
            s.EnableBreathingScale = true;
            // s.EnableGlint = true; // disabled: effect needs redesign
            // s.GlintInterval = 3.0;
            // s.GlintIntervalMax = 7.0;
            s.EnableParallax = true;
        }

        private void PresetCozyGlow_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Bloom;
            s.IconGlowSize = 5.0;
            s.IconGlowIntensity = 1.5;
            s.ColorPreset = ColorPreset.Sunset;
            s.EnablePulse = true;
            s.PulseSpeed = 5.0;
            s.PulseMinOpacity = 0.6;
            s.EnableBreathingScale = true;
            s.EnableLevitation = true;
            s.LevitationSpeed = 5.0;
            s.LevitationAmount = 2.0;
        }

        private void PresetNeonPulseClean_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Neon;
            s.IconGlowSize = 5.0;
            s.IconGlowIntensity = 2.5;
            s.ColorPreset = ColorPreset.Synthwave;
            s.EnablePulse = true;
            s.PulseSpeed = 3.0;
            s.PulseMinOpacity = 0.3;
            s.EnableColorCycle = true;
            s.ColorCycleSpeed = 6.0;
            s.EnableBreathingScale = true;
            s.EnableLevitation = true;
            s.LevitationSpeed = 4.0;
            s.LevitationAmount = 2.0;
        }

        private void PresetSubtleGleamClean_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Soft;
            s.IconGlowSize = 3.0;
            s.IconGlowIntensity = 1.2;
            s.EnableLevitation = true;
            s.LevitationSpeed = 7.0;
            s.LevitationAmount = 1.0;
        }

        private void PresetSynthwaveCruiserSquare_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowSize = 3.5;
            s.IconGlowIntensity = 2.5;
            s.ColorPreset = ColorPreset.Synthwave;
            s.EffectShape = EffectShape.Square;
            s.EnableShimmer = true;
            s.ShimmerSpeed = 4.0;
            s.ShimmerOpacity = 0.40;
            s.ShimmerPauseMin = 2.0;
            s.ShimmerPauseMax = 6.5;
            s.EnableTilt = true;
            s.EnablePulse = true;
            s.PulseSpeed = 6.0;
            s.PulseMinOpacity = 0.4;
            s.EnableHoverEffect = true;
            s.EnableLevitation = true;
            s.LevitationSpeed = 5.0;
            s.LevitationAmount = 2.0;
            s.EnableParallax = true;
        }

        private void PresetSynthwaveCruiserCircle_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowSize = 3.5;
            s.IconGlowIntensity = 2.5;
            s.ColorPreset = ColorPreset.Synthwave;
            s.EffectShape = EffectShape.Circular;
            s.EnableShimmer = true;
            s.ShimmerSpeed = 4.0;
            s.ShimmerOpacity = 0.40;
            s.ShimmerPauseMin = 2.0;
            s.ShimmerPauseMax = 6.5;
            s.EnableTilt = true;
            s.EnablePulse = true;
            s.PulseSpeed = 6.0;
            s.PulseMinOpacity = 0.4;
            s.EnableHoverEffect = true;
            s.EnableLevitation = true;
            s.LevitationSpeed = 5.0;
            s.LevitationAmount = 2.0;
            s.EnableParallax = true;
            s.Enable3DRotation = true;
            s.RotationSpeed = 7.1;
            s.RotationAmount = 2.5;
        }

        private void PresetFullSpectacle_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Star;
            s.IconGlowSize = 5.5;
            s.IconGlowIntensity = 2.5;
            s.EnableIconGlowSpin = true;
            s.IconGlowSpinSpeed = 15.0;
            s.EnableShimmer = true;
            s.ShimmerSpeed = 1.2;
            s.ShimmerOpacity = 0.90;
            s.ShimmerShineStyle = ShineStyle.Holographic;
            s.ShimmerPauseMin = 0.5;
            s.ShimmerPauseMax = 1.5;
            s.EnableTilt = true;
            s.EnablePulse = true;
            s.PulseSpeed = 4.0;
            s.PulseMinOpacity = 0.4;
            s.EnableColorCycle = true;
            s.ColorCycleSpeed = 8.0;
            s.EnableSparkles = true;
            s.SparkleCount = 20;
            s.SparkleSpeed = 1.0;
            s.EnableHoverEffect = true;
            s.EnableBreathingScale = true;
            s.EnableLevitation = true;
            s.LevitationSpeed = 3.5;
            s.LevitationAmount = 3.0;
            s.Enable3DRotation = true;
            s.RotationSpeed = 6.0;
            s.RotationAmount = 4.0;
            // s.EnableGlint = true; // disabled: effect needs redesign
            // s.GlintInterval = 2.0;
            // s.GlintIntervalMax = 5.0;
            s.EnableShadowDrift = true;
        }

        private void PresetCollectorsEdition_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowStyle = GlowStyle.Halo;
            s.IconGlowIntensity = 1.8;
            s.ColorPreset = ColorPreset.Gold;
            s.EnableShineSweep = true;
            s.ShineSweepSpeed = 4.0;
            s.ShineSweepPauseMin = 3.0;
            s.ShineSweepPauseMax = 6.0;
            s.EnableSparkles = true;
            s.SparkleCount = 6;
            s.SparkleSpeed = 0.5;
            s.EnableHoverEffect = true;
            s.EnableLevitation = true;
            s.LevitationSpeed = 6.0;
            s.LevitationAmount = 1.5;
            // s.EnableGlint = true; // disabled: effect needs redesign
            // s.GlintInterval = 6.0;
            // s.GlintIntervalMax = 14.0;
            s.EnableShadowDrift = true;
        }

        private void PresetRetroArcade_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSettingsOrNull(); if (s == null) return;
            ResetToCleanBaseline(s);
            s.IconGlowSize = 4.0;
            s.IconGlowIntensity = 2.8;
            s.EnableIconGlowSpin = true;
            s.IconGlowSpinSpeed = 10.0;
            s.ColorPreset = ColorPreset.NeonGreen;
            s.EnableShimmer = true;
            s.ShimmerSpeed = 1.0;
            s.ShimmerOpacity = 0.80;
            s.ShimmerPauseMin = 0.5;
            s.ShimmerPauseMax = 1.5;
            s.EnableTilt = true;
            s.EnablePulse = true;
            s.PulseSpeed = 2.0;
            s.PulseMinOpacity = 0.3;
            s.EnableColorCycle = true;
            s.ColorCycleSpeed = 5.0;
            s.EnableSparkles = true;
            s.SparkleCount = 25;
            s.SparkleSpeed = 1.5;
            // s.EnableGlint = true; // disabled: effect needs redesign
            // s.GlintInterval = 1.0;
            // s.GlintIntervalMax = 3.0;
            s.EnableParallax = true;
        }
    }
}
