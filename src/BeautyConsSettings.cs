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
        private double iconGlowSize = 3.5;
        private double iconGlowIntensity = 1.8;
        private bool enableIconGlowSpin = false;
        private double iconGlowSpinSpeed = 20.0;
        private ColorPreset colorPreset = ColorPreset.Auto;
        private string customColor1 = "#6495ED";
        private string customColor2 = "#B464FF";
        private bool enableShineSweep = false;
        private double shineSweepSpeed = 3.0;
        private bool enableShimmer = false;
        private double shimmerSpeed = 2.5;
        private double shimmerOpacity = 0.55;
        private bool enablePulse = false;
        private double pulseSpeed = 3.0;
        private double pulseMinOpacity = 0.4;
        private bool enableColorCycle = false;
        private double colorCycleSpeed = 8.0;
        private bool enableSparkles = false;
        private int sparkleCount = 12;
        private double sparkleSpeed = 1.0;

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

        public bool EnableIconGlowSpin
        {
            get => enableIconGlowSpin;
            set { enableIconGlowSpin = value; OnPropertyChanged(); }
        }

        public double IconGlowSpinSpeed
        {
            get => iconGlowSpinSpeed;
            set { iconGlowSpinSpeed = value; OnPropertyChanged(); }
        }

        public ColorPreset ColorPreset
        {
            get => colorPreset;
            set { colorPreset = value; OnPropertyChanged(); }
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

        public bool EnableShineSweep
        {
            get => enableShineSweep;
            set { enableShineSweep = value; OnPropertyChanged(); }
        }

        public double ShineSweepSpeed
        {
            get => shineSweepSpeed;
            set { shineSweepSpeed = value; OnPropertyChanged(); }
        }

        public bool EnableShimmer
        {
            get => enableShimmer;
            set { enableShimmer = value; OnPropertyChanged(); }
        }

        public double ShimmerSpeed
        {
            get => shimmerSpeed;
            set { shimmerSpeed = value; OnPropertyChanged(); }
        }

        public double ShimmerOpacity
        {
            get => shimmerOpacity;
            set { shimmerOpacity = value; OnPropertyChanged(); }
        }

        public bool EnablePulse
        {
            get => enablePulse;
            set { enablePulse = value; OnPropertyChanged(); }
        }

        public double PulseSpeed
        {
            get => pulseSpeed;
            set { pulseSpeed = value; OnPropertyChanged(); }
        }

        public double PulseMinOpacity
        {
            get => pulseMinOpacity;
            set { pulseMinOpacity = value; OnPropertyChanged(); }
        }

        public bool EnableColorCycle
        {
            get => enableColorCycle;
            set { enableColorCycle = value; OnPropertyChanged(); }
        }

        public double ColorCycleSpeed
        {
            get => colorCycleSpeed;
            set { colorCycleSpeed = value; OnPropertyChanged(); }
        }

        public bool EnableSparkles
        {
            get => enableSparkles;
            set { enableSparkles = value; OnPropertyChanged(); }
        }

        public int SparkleCount
        {
            get => sparkleCount;
            set { sparkleCount = value; OnPropertyChanged(); }
        }

        public double SparkleSpeed
        {
            get => sparkleSpeed;
            set { sparkleSpeed = value; OnPropertyChanged(); }
        }
    }
}
