using Blasphemy.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Items.Accesories
{
    public class PainAmulet : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.PanicNecklace}";
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var bp = player.GetModPlayer<BlasphemyPlayer>();
            
            bp.MaxPainMultiplier *= 1.5f;
            bp.PainGainMultiplier *= 1.2f;
        }
    }
}