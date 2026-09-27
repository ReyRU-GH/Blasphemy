using System;
using System.Collections.Generic;
using System.IO;
using Blasphemy.NPCs.Bosses.FossilBoss;
using Blasphemy.WorldGeneration.FossilArena;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace Blasphemy.Systems;

public sealed class FossilEncounterSystem : ModSystem
{
    public static bool RitualUnlocked { get; private set; }
    public static bool EncounterActive { get; private set; }
    public static bool LizardSpawnedThisFight { get; private set; }
    public static bool SharkSpawnedThisFight { get; private set; }

    public static bool HasArena => DesertCaveSystem.HasFossilArena;
    public static Rectangle ArenaBounds => DesertCaveSystem.FossilArenaBounds;
    public static Vector2 ArenaCenter => DesertCaveSystem.FossilArenaCenter;

    public override void ClearWorld()
    {
        RitualUnlocked = false;
        EncounterActive = false;
        LizardSpawnedThisFight = false;
        SharkSpawnedThisFight = false;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        if (RitualUnlocked)
            tag["FossilRitualUnlocked"] = true;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        RitualUnlocked = tag.ContainsKey("FossilRitualUnlocked") && tag.GetBool("FossilRitualUnlocked");
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(RitualUnlocked);
    }

    public override void NetReceive(BinaryReader reader)
    {
        RitualUnlocked = reader.ReadBoolean();
    }

    public static bool RequestStartFromArchaeologistQuest(Player player)
    {
        if (player is null || !player.active || player.dead || !HasArena || IsEncounterNpcAlive() ||
            !IsInsideArena(player.Center, padding: 120f))
            return false;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (player.whoAmI != Main.myPlayer)
                return false;

            ModPacket packet = Blasphemy.Instance.GetPacket();
            packet.Write((byte)BlasphemyMessageType.FossilQuestSummonRequest);
            packet.Write((byte)player.whoAmI);
            packet.Send();
            return true;
        }

        return TryStartFromArchaeologistQuest(player);
    }

    public static bool RequestStartFromSummonItem(Player player)
    {
        // TODO: !RitualUnlocked
        if ( player is null || !player.active || player.dead || !HasArena || IsEncounterNpcAlive())
            return false;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (player.whoAmI != Main.myPlayer)
                return false;

            ModPacket packet = Blasphemy.Instance.GetPacket();
            packet.Write((byte)BlasphemyMessageType.FossilSummonItemRequest);
            packet.Write((byte)player.whoAmI);
            packet.Send();
            return true;
        }

        return TryStartFromSummonItem(player);
    }
    
    public static bool TryStartFromArchaeologistQuest(Player player)
    {
        if (!CanStart(player) || !IsInsideArena(player.Center, padding: 120f))
            return false;

        RitualUnlocked = true;
        StartEncounter(player, carryPlayerToArena: false);
        SyncWorldFlags();
        return true;
    }
    
    public static bool TryStartFromSummonItem(Player player)
    {
        // TODO: Add !RitualUnlocked after adding Wizard NPC or smth like that
        if (!CanStart(player))
            return false;

        bool carryPlayerToArena = !IsInsideArena(player.Center, padding: 80f);
        StartEncounter(player, carryPlayerToArena);
        return true;
    }

    public static bool IsInsideArena(Vector2 worldPosition, float padding = 0f)
    {
        if (!HasArena)
            return false;

        Rectangle bounds = ArenaBounds;
        bounds.Inflate((int)padding, (int)padding);
        return bounds.Contains(worldPosition.ToPoint());
    }

    public static bool IsEncounterNpcAlive()
    {
        return NPC.AnyNPCs(ModContent.NPCType<FossilSnakeHead>()) ||
               NPC.AnyNPCs(ModContent.NPCType<FossilLizard>()) ||
               NPC.AnyNPCs(ModContent.NPCType<FossilFish>());
    }

    public static bool IsSharkAlone()
    {
        return NPC.AnyNPCs(ModContent.NPCType<FossilFish>()) &&
               !NPC.AnyNPCs(ModContent.NPCType<FossilSnakeHead>()) &&
               !NPC.AnyNPCs(ModContent.NPCType<FossilLizard>());
    }

    public static void EnsureLizardSpawned(Player target)
    {
        if (LizardSpawnedThisFight || Main.netMode == NetmodeID.MultiplayerClient)
            return;

        LizardSpawnedThisFight = true;
        Vector2 spawn = HasArena ? ArenaCenter + new Vector2(0f, -220f) : target.Center + new Vector2(0f, -220f);

        NPC.NewNPC(
            target.GetSource_Misc("FossilEncounterLizard"),
            (int)spawn.X,
            (int)spawn.Y,
            ModContent.NPCType<FossilLizard>(),
            Target: target.whoAmI
        );
    }

    public static void EnsureSharkSpawned(Player target)
    {
        if (SharkSpawnedThisFight || Main.netMode == NetmodeID.MultiplayerClient)
            return;

        SharkSpawnedThisFight = true;
        Vector2 spawn = HasArena ? ArenaCenter + new Vector2(0f, 340f) : target.Center + new Vector2(0f, 340f);

        NPC.NewNPC(
            target.GetSource_Misc("FossilEncounterShark"),
            (int)spawn.X,
            (int)spawn.Y,
            ModContent.NPCType<FossilFish>(),
            Target: target.whoAmI
        );
    }

    private static bool CanStart(Player player)
    {
        if (player is null || !player.active || player.dead || !HasArena)
            return false;

        return !IsEncounterNpcAlive();
    }

    private static void StartEncounter(Player player, bool carryPlayerToArena)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        EncounterActive = true;
        LizardSpawnedThisFight = false;
        SharkSpawnedThisFight = false;

        Vector2 spawnPosition = player.Center + new Vector2(0f, 760f);

        NPC.NewNPC(
            player.GetSource_Misc("FossilEncounterSnake"),
            (int)spawnPosition.X,
            (int)spawnPosition.Y,
            ModContent.NPCType<FossilSnakeHead>(),
            Start: 0,
            ai0: FossilSnakeHead.StateAwakening,
            ai1: 0f,
            ai2: 0f,
            ai3: carryPlayerToArena ? FossilSnakeHead.SummonModeCarry : FossilSnakeHead.SummonModeDirect,
            Target: player.whoAmI
        );
    }

    public override void PostUpdateNPCs()
    {
        bool anyEncounterNpc = IsEncounterNpcAlive();

        if (EncounterActive && !anyEncounterNpc)
        {
            EncounterActive = false;
            LizardSpawnedThisFight = false;
            SharkSpawnedThisFight = false;
        }
        else if (anyEncounterNpc)
        {
            EncounterActive = true;
        }
    }

    public override void ModifyLightingBrightness(ref float scale)
    {
        if (Main.dedServ || !ShouldApplyDarknessToLocalPlayer())
            return;

        scale *= IsSharkAlone() ? 0.90f : 0.94f;
    }

    public override void PostUpdatePlayers()
    {
        if (Main.dedServ || !ShouldApplyDarknessToLocalPlayer())
            return;

        // A warm personal glow, comparable to the Shine buff, follows the local player.
        Lighting.AddLight(Main.LocalPlayer.Center, 1.35f, 1.20f, 0.94f);

        if (!NPC.AnyNPCs(ModContent.NPCType<FossilFish>()))
            return;

        Player player = Main.LocalPlayer;
        float surfaceY = player.Bottom.Y + 48f;
        Vector2 viewTopLeft = Main.screenPosition;
        for (int i = 0; i < 18; i++)
        {
            Vector2 position = viewTopLeft + new Vector2(
                Main.rand.NextFloat(-80f, Main.screenWidth + 80f),
                Main.rand.NextFloat(-80f, Main.screenHeight + 80f));
            Dust dust = Dust.NewDustPerfect(position, i % 4 == 0 ? DustID.SandSpray : DustID.Sandstorm,
                new Vector2(Main.rand.NextFloat(5f, 9f), Main.rand.NextFloat(-1.4f, 0.8f)),
                145, default, Main.rand.NextFloat(0.65f, 1.2f));
            dust.noGravity = true;
        }

        for (int i = 0; i < 14; i++)
        {
            Vector2 position = new Vector2(
                player.Center.X + Main.rand.NextFloat(-Main.screenWidth * 0.45f, Main.screenWidth * 0.45f),
                surfaceY + Main.rand.NextFloat(-30f, 100f));
            Dust dust = Dust.NewDustPerfect(position, DustID.Sandstorm,
                new Vector2(Main.rand.NextFloat(2f, 6f), Main.rand.NextFloat(-2.5f, 0f)),
                100, default, Main.rand.NextFloat(1f, 1.6f));
            dust.noGravity = true;
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        layers.Insert(0, new LegacyGameInterfaceLayer(
            "Blasphemy: Fossil Sand Veil",
            DrawSandVeil
        ));
    }

    private static bool DrawSandVeil()
    {
        if (Main.gameMenu || !ShouldApplyDarknessToLocalPlayer() || !NPC.AnyNPCs(ModContent.NPCType<FossilFish>()))
            return true;

        SpriteBatch spriteBatch = Main.spriteBatch;
        Texture2D pixel = TextureAssets.MagicPixel.Value;

        spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
            new Color(178, 143, 94) * 0.17f);

        int surface = (int)(Main.LocalPlayer.Bottom.Y + 48f - Main.screenPosition.Y);
        for (int i = 0; i < 24; i++)
        {
            int y = surface - 40 + i * 10;
            if (y < 0 || y >= Main.screenHeight)
                continue;

            float opacity = 0.02f + i / 23f * 0.21f;
            spriteBatch.Draw(pixel, new Rectangle(0, y, Main.screenWidth, Math.Min(10, Main.screenHeight - y)),
                new Color(180, 143, 93) * opacity);
        }

        int denseTop = Math.Clamp(surface + 200, 0, Main.screenHeight);
        if (denseTop < Main.screenHeight)
            spriteBatch.Draw(pixel, new Rectangle(0, denseTop, Main.screenWidth, Main.screenHeight - denseTop),
                new Color(180, 143, 93) * 0.23f);

        return true;
    }

    public static bool ShouldApplyDarknessToLocalPlayer()
    {
        if (!EncounterActive || Main.LocalPlayer is null || !Main.LocalPlayer.active)
            return false;

        if (!HasArena)
            return true;

        Rectangle expandedArena = ArenaBounds;
        expandedArena.Inflate(900, 700);
        return expandedArena.Contains(Main.LocalPlayer.Center.ToPoint());
    }

    private static void SyncWorldFlags()
    {
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.WorldData);
    }
}
