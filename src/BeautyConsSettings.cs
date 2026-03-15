using BeautyCons.IconGlow;
using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;

namespace BeautyCons
{
    public class BeautyConsSettings : ObservableObject
    {
        private bool enablePlugin = true;
        private bool enableDebugLogging = false;
        private bool enableIconGlow = true;
        private GlowStyle iconGlowStyle = GlowStyle.Neon;
        private double iconGlowSize = 6.0;
        private double iconGlowIntensity = 1.8;
        private bool useCustomColors = false;
        private string customColor1 = "#6495ED";
        private string customColor2 = "#B464FF";

        public bool EnablePlugin
        {
            get => enablePlugin;
            set { enablePlugin = value; OnPropertyChanged(); }
        }

        public bool EnableDebugLogging
        {
            get => enableDebugLogging;
            set { enableDebugLogging = value; OnPropertyChanged(); }
        }

        public bool EnableIconGlow
        {
            get => enableIconGlow;
            set { enableIconGlow = value; OnPropertyChanged(); }
        }

        public GlowStyle IconGlowStyle
        {
            get => iconGlowStyle;
            set { iconGlowStyle = value; OnPropertyChanged(); }
        }

        public double IconGlowSize
        {
            get => iconGlowSize;
            set { iconGlowSize = value; OnPropertyChanged(); }
        }

        public double IconGlowIntensity
        {
            get => iconGlowIntensity;
            set { iconGlowIntensity = value; OnPropertyChanged(); }
        }

        public bool UseCustomColors
        {
            get => useCustomColors;
            set { useCustomColors = value; OnPropertyChanged(); }
        }

        public string CustomColor1
        {
            get => customColor1;
            set { customColor1 = value; OnPropertyChanged(); }
        }

        public string CustomColor2
        {
            get => customColor2;
            set { customColor2 = value; OnPropertyChanged(); }
        }
    }
}
