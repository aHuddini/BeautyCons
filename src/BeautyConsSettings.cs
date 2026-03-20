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
        private EffectShape effectShape = EffectShape.Square;
        private bool enableShineSweep = false;
        private double shineSweepSpeed = 3.0;
        private double shineSweepPauseMin = 1.0;
        private double shineSweepPauseMax = 4.0;
        private bool enableTilt = false;
        private double tiltSpeed = 2.5;
        private double tiltPauseMin = 1.0;
        private double tiltPauseMax = 4.0;
        private bool enableShimmer = false;
        private double shimmerSpeed = 1.5;
        private double shimmerOpacity = 0.85;
        private double shimmerPauseMin = 1.0;
        private double shimmerPauseMax = 4.0;
        private ShineStyle shimmerShineStyle = ShineStyle.White;
        private bool enableColorShimmer = false;
        private double colorShimmerSpeed = 1.5;
        private double colorShimmerOpacity = 0.65;
        private bool enablePulse = false;
        private double pulseSpeed = 3.0;
        private double pulseMinOpacity = 0.4;
        private bool enableColorCycle = false;
        private double colorCycleSpeed = 8.0;
        private bool enableSparkles = false;
        private int sparkleCount = 12;
        private double sparkleSpeed = 1.0;
        private bool enableHoverEffect = false;
        private bool enableBreathingScale = false;
        private bool enableLevitation = false;
        private double levitationSpeed = 4.0;
        private double levitationAmount = 2.5;
        private bool enable3DRotation = false;
        private double rotationSpeed = 6.0;
        private double rotationAmount = 3.0;
        private bool enableGlint = false;
        private double glintIntervalMin = 4.0;
        private double glintIntervalMax = 10.0;
        private bool enableShadowDrift = false;
        private bool enableParallax = false;

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

        public EffectShape EffectShape
        {
            get => effectShape;
            set { effectShape = value; OnPropertyChanged(); }
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

        public double ShineSweepPauseMin
        {
            get => shineSweepPauseMin;
            set { shineSweepPauseMin = value; OnPropertyChanged(); }
        }

        public double ShineSweepPauseMax
        {
            get => shineSweepPauseMax;
            set { shineSweepPauseMax = value; OnPropertyChanged(); }
        }

        public bool EnableTilt
        {
            get => enableTilt;
            set { enableTilt = value; OnPropertyChanged(); }
        }

        public double TiltSpeed
        {
            get => tiltSpeed;
            set { tiltSpeed = value; OnPropertyChanged(); }
        }

        public double TiltPauseMin
        {
            get => tiltPauseMin;
            set { tiltPauseMin = value; OnPropertyChanged(); }
        }

        public double TiltPauseMax
        {
            get => tiltPauseMax;
            set { tiltPauseMax = value; OnPropertyChanged(); }
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

        public double ShimmerPauseMin
        {
            get => shimmerPauseMin;
            set { shimmerPauseMin = value; OnPropertyChanged(); }
        }

        public double ShimmerPauseMax
        {
            get => shimmerPauseMax;
            set { shimmerPauseMax = value; OnPropertyChanged(); }
        }

        public ShineStyle ShimmerShineStyle
        {
            get => shimmerShineStyle;
            set { shimmerShineStyle = value; OnPropertyChanged(); }
        }

        public bool EnableColorShimmer
        {
            get => enableColorShimmer;
            set { enableColorShimmer = value; OnPropertyChanged(); }
        }

        public double ColorShimmerSpeed
        {
            get => colorShimmerSpeed;
            set { colorShimmerSpeed = value; OnPropertyChanged(); }
        }

        public double ColorShimmerOpacity
        {
            get => colorShimmerOpacity;
            set { colorShimmerOpacity = value; OnPropertyChanged(); }
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

        public bool EnableHoverEffect
        {
            get => enableHoverEffect;
            set { enableHoverEffect = value; OnPropertyChanged(); }
        }

        public bool EnableBreathingScale
        {
            get => enableBreathingScale;
            set { enableBreathingScale = value; OnPropertyChanged(); }
        }

        public bool EnableLevitation
        {
            get => enableLevitation;
            set { enableLevitation = value; OnPropertyChanged(); }
        }

        public double LevitationSpeed
        {
            get => levitationSpeed;
            set { levitationSpeed = value; OnPropertyChanged(); }
        }

        public double LevitationAmount
        {
            get => levitationAmount;
            set { levitationAmount = value; OnPropertyChanged(); }
        }

        public bool Enable3DRotation
        {
            get => enable3DRotation;
            set { enable3DRotation = value; OnPropertyChanged(); }
        }

        public double RotationSpeed
        {
            get => rotationSpeed;
            set { rotationSpeed = value; OnPropertyChanged(); }
        }

        public double RotationAmount
        {
            get => rotationAmount;
            set { rotationAmount = value; OnPropertyChanged(); }
        }

        public bool EnableGlint
        {
            get => enableGlint;
            set { enableGlint = value; OnPropertyChanged(); }
        }

        public double GlintIntervalMin
        {
            get => glintIntervalMin;
            set { glintIntervalMin = value; OnPropertyChanged(); }
        }

        public double GlintIntervalMax
        {
            get => glintIntervalMax;
            set { glintIntervalMax = value; OnPropertyChanged(); }
        }

        public bool EnableShadowDrift
        {
            get => enableShadowDrift;
            set { enableShadowDrift = value; OnPropertyChanged(); }
        }

        public bool EnableParallax
        {
            get => enableParallax;
            set { enableParallax = value; OnPropertyChanged(); }
        }
    }
}
