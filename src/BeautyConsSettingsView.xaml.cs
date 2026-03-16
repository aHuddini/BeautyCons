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
        }
    }
}
