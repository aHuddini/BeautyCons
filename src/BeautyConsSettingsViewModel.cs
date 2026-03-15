using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;

namespace BeautyCons
{
    public class BeautyConsSettingsViewModel : ObservableObject, ISettings
    {
        private readonly BeautyCons plugin;
        private BeautyConsSettings settings;

        public BeautyConsSettingsViewModel(BeautyCons plugin)
        {
            this.plugin = plugin;

            var savedSettings = plugin.LoadPluginSettings<BeautyConsSettings>();
            settings = savedSettings ?? new BeautyConsSettings();
        }

        public IPlayniteAPI PlayniteApi => plugin.PlayniteApi;

        public BeautyConsSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public void BeginEdit()
        {
            // Called when settings view is opened
        }

        public void CancelEdit()
        {
            var savedSettings = plugin.LoadPluginSettings<BeautyConsSettings>();
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
