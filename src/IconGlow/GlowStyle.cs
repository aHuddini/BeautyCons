using SkiaSharp;

namespace BeautyCons.IconGlow
{
    public enum GlowStyle
    {
        Neon,
        Soft,
        Sharp
    }

    public struct GlowLayer
    {
        public float SigmaMultiplier;
        public float Alpha;

        public GlowLayer(float sigmaMultiplier, float alpha)
        {
            SigmaMultiplier = sigmaMultiplier;
            Alpha = alpha;
        }
    }

    public struct GlowStyleParams
    {
        public GlowLayer[] Layers;
        public float ColorShiftFactor;
        public SKBlendMode BlendMode;

        public static GlowStyleParams GetParams(GlowStyle style)
        {
            switch (style)
            {
                case GlowStyle.Neon:
                    return new GlowStyleParams
                    {
                        Layers = new[]
                        {
                            new GlowLayer(2.0f, 0.5f),
                            new GlowLayer(1.0f, 0.7f),
                            new GlowLayer(0.5f, 0.9f)
                        },
                        ColorShiftFactor = 0.4f,
                        BlendMode = SKBlendMode.Plus
                    };

                case GlowStyle.Soft:
                    return new GlowStyleParams
                    {
                        Layers = new[]
                        {
                            new GlowLayer(2.5f, 0.6f)
                        },
                        ColorShiftFactor = 0.8f,
                        BlendMode = SKBlendMode.SrcOver
                    };

                case GlowStyle.Sharp:
                    return new GlowStyleParams
                    {
                        Layers = new[]
                        {
                            new GlowLayer(0.4f, 0.9f),
                            new GlowLayer(0.8f, 0.3f)
                        },
                        ColorShiftFactor = 0.2f,
                        BlendMode = SKBlendMode.Plus
                    };

                default:
                    goto case GlowStyle.Neon;
            }
        }
    }
}
