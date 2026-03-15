using System;
using System.Windows;
using System.Windows.Controls;

namespace BeautyCons
{
    public partial class BeautyConsSettingsView : UserControl
    {
        private readonly BeautyCons _plugin;

        public BeautyConsSettingsView(BeautyCons plugin)
        {
            _plugin = plugin;
            InitializeComponent();
            // DO NOT set DataContext manually - Playnite sets it automatically
            // to the ISettings object returned by GetSettings()
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
            vm.Settings.IconGlowStyle = IconGlow.GlowStyle.Neon;
            vm.Settings.IconGlowSize = 6.0;
            vm.Settings.IconGlowIntensity = 1.8;
            vm.Settings.UseCustomColors = false;
            vm.Settings.CustomColor1 = "#6495ED";
            vm.Settings.CustomColor2 = "#B464FF";
        }
    }
}
