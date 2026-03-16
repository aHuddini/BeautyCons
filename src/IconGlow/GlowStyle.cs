using SkiaSharp;

namespace BeautyCons.IconGlow
{
    public enum GlowStyle
    {
        Neon,
        Soft,
        Sharp,
        Diamond,
        Cross,
        Star,
        Bloom,
        Halo
    }

    public enum GlowShape
    {
        Normal,
        Diamond,
        Cross,
        Star
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
        public GlowShape Shape;

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
                        BlendMode = SKBlendMode.Plus,
                        Shape = GlowShape.Normal
                    };

                case GlowStyle.Soft:
                    return new GlowStyleParams
                    {
                        Layers = new[]
                        {
                            new GlowLayer(1.8f, 0.5f)
                        },
                        ColorShiftFactor = 0.6f,
                        BlendMode = SKBlendMode.SrcOver,
                        Shape = GlowShape.Normal
                    };

                case GlowStyle.Sharp:
                    return new GlowStyleParams
                    {
                        Layers = new[]
                        {
                            new GlowLayer(0.25f, 1.0f),
                            new GlowLayer(0.5f, 0.6f)
                        },
                        ColorShiftFactor = 0.15f,
                        BlendMode = SKBlendMode.Plus,
                        Shape = GlowShape.Normal
                    };

                case GlowStyle.Diamond:
                    return new GlowStyleParams
                    {
                        Layers = new[]
                        {
                            new GlowLayer(1.5f, 0.6f),
                            new GlowLayer(0.7f, 0.8f)
                        },
                        ColorShiftFactor = 0.3f,
                        BlendMode = SKBlendMode.Plus,
                        Shape = GlowShape.Diamond
                    };

                case GlowStyle.Cross:
                    return new GlowStyleParams
                    {
                        Layers = new[]
                        {
                            new GlowLayer(1.5f, 0.6f),
                            new GlowLayer(0.7f, 0.8f)
                        },
                        ColorShiftFactor = 0.3f,
                        BlendMode = SKBlendMode.Plus,
                        Shape = GlowShape.Cross
                    };

                case GlowStyle.Star:
                    return new GlowStyleParams
                    {
                        Layers = new[]
                        {
                            new GlowLayer(1.8f, 0.5f),
                            new GlowLayer(0.8f, 0.8f)
                        },
                        ColorShiftFactor = 0.35f,
                        BlendMode = SKBlendMode.Plus,
                        Shape = GlowShape.Star
                    };

                case GlowStyle.Bloom:
                    // Overexposed look: very bright tight center + wide soft falloff
                    return new GlowStyleParams
                    {
                        Layers = new[]
                        {
                            new GlowLayer(3.0f, 0.3f),
                            new GlowLayer(1.5f, 0.5f),
                            new GlowLayer(0.3f, 1.0f)
                        },
                        ColorShiftFactor = 0.2f,
                        BlendMode = SKBlendMode.Plus,
                        Shape = GlowShape.Normal
                    };

                case GlowStyle.Halo:
                    // Ring at fixed distance, no inner fill — achieved via
                    // wide blur minus tight blur (outer layer strong, inner cancels)
                    return new GlowStyleParams
                    {
                        Layers = new[]
                        {
                            new GlowLayer(1.8f, 0.7f),
                            new GlowLayer(1.0f, 0.5f),
                            new GlowLayer(0.3f, 0.15f)
                        },
                        ColorShiftFactor = 0.5f,
                        BlendMode = SKBlendMode.SrcOver,
                        Shape = GlowShape.Normal
                    };

                default:
                    goto case GlowStyle.Neon;
            }
        }
    }
}
