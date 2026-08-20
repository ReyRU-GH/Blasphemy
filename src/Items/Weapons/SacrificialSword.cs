using Blasphemy.DamageClass;
using Blasphemy.Players;
using Blasphemy.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Items.Weapons
{
    public class GlassShard : ModItem, BlasphemySystem.ILifeCostItem, BlasphemySystem.IPainWeapon, BlasphemySystem.IConditionalActivation
    {
        public int LifeCost => 5;
        public int RecoveryPercent => 20;
        public int PainGain => 5;
        
        public override string Texture => $"Terraria/Images/Item_{ItemID.Muramasa}";
        
        public bool ActivateOnHitOnly => true;

        public override void SetDefaults()
        {
            Item.damage = 15;
            Item.DamageType = ModContent.GetInstance<SacrificialDamage>();
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shootSpeed = 10f;
        }
    }
}