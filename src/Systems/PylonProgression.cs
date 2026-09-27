using System.IO;
using Blasphemy.Items.Pylons;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Blasphemy.Systems;

public sealed class PylonProgression : ModSystem
{
    public static bool DownedEaterOfWorlds { get; private set; }
    public static bool DownedBrainOfCthulhu { get; private set; }
    public static bool CanBuyCorruptPylon => DownedEaterOfWorlds || NPC.downedBoss2 && !WorldGen.crimson;
    public static bool CanBuyCrimsonPylon => DownedBrainOfCthulhu || NPC.downedBoss2 && WorldGen.crimson;

    public override void OnWorldLoad() => DownedEaterOfWorlds = DownedBrainOfCthulhu = false;
    public override void OnWorldUnload() => DownedEaterOfWorlds = DownedBrainOfCthulhu = false;

    public override void SaveWorldData(TagCompound tag)
    {
        if (DownedEaterOfWorlds) tag["DownedEaterOfWorlds"] = true;
        if (DownedBrainOfCthulhu) tag["DownedBrainOfCthulhu"] = true;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        DownedEaterOfWorlds = tag.GetBool("DownedEaterOfWorlds");
        DownedBrainOfCthulhu = tag.GetBool("DownedBrainOfCthulhu");
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(DownedEaterOfWorlds);
        writer.Write(DownedBrainOfCthulhu);
    }

    public override void NetReceive(BinaryReader reader)
    {
        DownedEaterOfWorlds = reader.ReadBoolean();
        DownedBrainOfCthulhu = reader.ReadBoolean();
    }

    public static void RecordDefeat(int npcType)
    {
        if (npcType == NPCID.BrainofCthulhu)
        {
            if (DownedBrainOfCthulhu) return;
            DownedBrainOfCthulhu = true;
        }
        else
        {
            if (DownedEaterOfWorlds) return;
            DownedEaterOfWorlds = true;
        }

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.WorldData);
    }
}

public sealed class PylonBossAndShop : GlobalNPC
{
    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (npc.type == NPCID.BrainofCthulhu)
        {
            PylonProgression.RecordDefeat(npc.type);
            return;
        }

        if (npc.type != NPCID.EaterofWorldsHead && npc.type != NPCID.EaterofWorldsBody &&
            npc.type != NPCID.EaterofWorldsTail) return;

        foreach (NPC other in Main.npc)
        {
            if (!other.active || other.whoAmI == npc.whoAmI) continue;
            if (other.type == NPCID.EaterofWorldsHead || other.type == NPCID.EaterofWorldsBody ||
                other.type == NPCID.EaterofWorldsTail) return;
        }
        PylonProgression.RecordDefeat(npc.type);
    }

    public override void ModifyShop(NPCShop shop)
    {
        shop.Add(new Item(ModContent.ItemType<SpacePylon>())
            { shopCustomPrice = Item.buyPrice(gold: 10) }, Condition.InSpace);

        if (shop.NpcType != NPCID.Dryad) return;

        shop.Add(new Item(ModContent.ItemType<CorruptPylon>())
            { shopCustomPrice = Item.buyPrice(gold: 25) },
            new Condition("Mods.Blasphemy.Conditions.DownedEaterOfWorlds", () => PylonProgression.CanBuyCorruptPylon));
        shop.Add(new Item(ModContent.ItemType<CrimsonPylon>())
            { shopCustomPrice = Item.buyPrice(gold: 25) },
            new Condition("Mods.Blasphemy.Conditions.DownedBrainOfCthulhu", () => PylonProgression.CanBuyCrimsonPylon));
    }
}
