using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Blasphemy.Config
{
    public class BlasphemyConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;
        public static BlasphemyConfig Instance => ModContent.GetInstance<BlasphemyConfig>();

        [Header("PainBarUI")]

        [DefaultValue(50f)]
        [Range(0f, 100f)]
        [Increment(0.1f)]
        public float PainBarPosX = 50f; // Процент по горизонтали

        [DefaultValue(85f)]
        [Range(0f, 100f)]
        [Increment(0.1f)]
        public float PainBarPosY = 85f; // Процент по вертикали

        [DefaultValue(1.0f)]
        [Range(0.5f, 3.0f)]
        [Increment(0.1f)]
        public float PainBarScale = 1.0f;

        [DefaultValue(true)]
        public bool ShowPainBar = true;

        [DefaultValue(false)]
        public bool LockPainBarPosition = false;
    }
}