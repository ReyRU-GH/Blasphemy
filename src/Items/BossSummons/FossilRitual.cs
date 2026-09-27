using Blasphemy.NPCs.Bosses.FossilBoss;
using Blasphemy.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Items.BossSummons
{
    public class FossilRitual : ModItem
    {
        public override string Texture =>
            "Blasphemy/Assets/Textures/Items/BossSummons/FossilRitual";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.maxStack = 20;
            Item.consumable = true;

            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 45;
            Item.useAnimation = 45;

            Item.UseSound = SoundID.Item44;

            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(silver: 50);
        }

        public override bool CanUseItem(Player player)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<FossilSnakeHead>()))
                return false;

            if (NPC.AnyNPCs(ModContent.NPCType<FossilLizard>()))
                return false;

            if (NPC.AnyNPCs(ModContent.NPCType<FossilFish>()))
                return false;

            // The summon item is intended to become available only
            // after the first archaeologist quest encounter.
            //if (!FossilEncounterSystem.RitualUnlocked)
                //return false;

            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                FossilEncounterSystem.RequestStartFromSummonItem(player);
            }

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.DesertFossil, 20)
                .AddIngredient(ItemID.Bone, 10)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}