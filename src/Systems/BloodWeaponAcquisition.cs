using Blasphemy.Items.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Systems;

public sealed class BloodWeaponLootAndShop : GlobalNPC
{
    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        if (npc.type == NPCID.CursedSkull)
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<VoodooDoll>(), 15));
    }

    public override void ModifyShop(NPCShop shop)
    {
        if (shop.NpcType == NPCID.Dryad)
            shop.Add(new Item(ModContent.ItemType<RitualDagger>()) { shopCustomPrice = Item.buyPrice(gold: 20) });
    }
}

public sealed class GraveNeedleWorldSpawn : ModSystem
{
    public override void PostUpdateWorld()
    {
        if (Main.GameUpdateCount % 3600 != 0) return;
        int type = ModContent.ItemType<GraveNeedle>();

        foreach (Player player in Main.player)
        {
            if (!player.active || player.dead || !player.ZoneGraveyard || !Main.rand.NextBool(2)) continue;
            bool nearby = false;
            foreach (Item item in Main.item)
                if (item.active && item.type == type && Vector2.DistanceSquared(item.Center, player.Center) < 1200f * 1200f)
                { nearby = true; break; }
            if (nearby) continue;

            int startX = (int)(player.Center.X / 16f) + Main.rand.Next(-28, 29);
            int startY = (int)(player.Center.Y / 16f) - 8;
            for (int y = startY; y < startY + 30; y++)
            {
                if (!WorldGen.InWorld(startX, y, 10) || !WorldGen.SolidTile(startX, y)) continue;
                if (WorldGen.SolidTile(startX, y - 1)) break;
                Item.NewItem(player.GetSource_Misc("GraveNeedle"), new Vector2(startX * 16f, (y - 1) * 16f), type);
                break;
            }
        }
    }
}
