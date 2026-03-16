using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using Playnite.SDK.Events;
using BeautyCons.IconGlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BeautyCons
{
    public class BeautyCons : GenericPlugin
    {
        private readonly IPlayniteAPI _api;
        private IconGlowManager _iconGlowManager;
        private BeautyConsSettingsViewModel _settingsViewModel;

        static BeautyCons()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    string assemblyName = new AssemblyName(args.Name).Name;
                    string extensionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    if (string.IsNullOrEmpty(extensionPath)) return null;

                    string dllPath = Path.Combine(extensionPath, $"{assemblyName}.dll");
                    if (File.Exists(dllPath))
                        return Assembly.LoadFrom(dllPath);
                }
                catch { }
                return null;
            };
        }

        public override Guid Id { get; } = Guid.Parse("eb7017af-c0ee-4416-aec5-27d516530af7");

        public BeautyCons(IPlayniteAPI api) : base(api)
        {
            _api = api;
            Properties = new GenericPluginProperties { HasSettings = true };

            _settingsViewModel = new BeautyConsSettingsViewModel(this);
            _iconGlowManager = new IconGlowManager(_settingsViewModel, api.Paths.ExtensionsDataPath);
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Defer initial glow to let Playnite finish rendering
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                var selectedGames = _api.MainView.SelectedGames;
                if (selectedGames != null && selectedGames.Any())
                {
                    _iconGlowManager.OnGameSelected(selectedGames.First());
                }
            };
            timer.Start();
        }

        // Playnite calls this virtual method when the user selects a different game
        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            if (args.NewValue != null && args.NewValue.Count > 0)
            {
                _iconGlowManager.OnGameSelected(args.NewValue[0]);
            }
            else
            {
                _iconGlowManager.OnGameSelected(null);
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            _iconGlowManager?.Destroy();
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return _settingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new BeautyConsSettingsView(this);
        }
    }
}
