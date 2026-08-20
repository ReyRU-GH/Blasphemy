using Terraria.ModLoader;

namespace Blasphemy.DamageClass
{
    public class SacrificialDamage : Terraria.ModLoader.DamageClass
    {
        internal static SacrificialDamage Instance;

        public override void Load() => Instance = this;
        public override void Unload() => Instance = null;

        public override StatInheritanceData GetModifierInheritance(Terraria.ModLoader.DamageClass damageClass)
        {
            return StatInheritanceData.None;
        }
    }
}