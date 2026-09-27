using System;
using System.IO;
using Blasphemy.Items.Weapons;
using Blasphemy.Projectiles;
using Blasphemy.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Blasphemy.NPCs.Bosses.FossilBoss;

[AutoloadBossHead]
public sealed class FossilSnakeHead : ModNPC
{
    public const int StateAwakening = 0;
    public const int StateTransportApproach = 1;
    public const int StateTransportCarry = 2;
    public const int StateOrbit = 3;
    public const int StatePetrify = 4;
    public const int StateDashTelegraph = 5;
    public const int StateDash = 6;
    public const int StateCombatGrab = 7;
    public const int StateBurrow = 8;
    public const int StateEmerge = 9;

    public const float SummonModeDirect = 0f;
    public const float SummonModeCarry = 1f;

    private const string SpriteRoot = "Blasphemy/Assets/Textures/Bosses/Fossil/Sprites/";

    // Keep the ribs overlapping so the spine reads as one continuous creature.
    private const int SegmentCount = 60;
    private const float SegmentSpacing = 29f;
    private const float HeadToBodySpacing = 53f;
    private const float TailSpacing = 25f;

    private const float OrbitRadiusX = 720f;
    private const float OrbitRadiusY = 420f;
    private const float OrbitSpeed = 9f;
    private const float DashSpeed = 48f;
    private const int DashTelegraphTicks = 90;
    private const int PetrifyTicks = 100;
    private const int RamTelegraphTicks = 32;

    private Vector2 _dashStart;
    private Vector2 _dashEnd;
    private Vector2 _grabDestination;
    private Vector2 _burrowTarget;
    private bool _rammedTarget;
    private int _burrowWait;
    private readonly int[] _segmentSlots = new int[SegmentCount];

    public override string Texture => SpriteRoot + "SnakeSkull";
    public override string BossHeadTexture => "Blasphemy/Assets/Textures/Bosses/Fossil/SnakeHead";

    public int CurrentState => (int)NPC.ai[0];
    public bool IsCarryingTarget => CurrentState == StateTransportCarry;

    public override void SetStaticDefaults()
    {
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.MustAlwaysDraw[Type] = true;
        NPCID.Sets.BossBestiaryPriority.Add(Type);
    }

    public override void SetDefaults()
    {
        NPC.width = 76;
        NPC.height = 92;
        NPC.damage = 42;
        NPC.defense = 10;
        NPC.lifeMax = 5600;
        NPC.aiStyle = -1;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.lavaImmune = true;
        NPC.trapImmune = true;
        NPC.knockBackResist = 0f;
        NPC.behindTiles = true;
        NPC.boss = true;
        NPC.BossBar = ModContent.GetInstance<FossilSnakeBossBar>();
        NPC.npcSlots = 200f;
        NPC.netAlways = true;
    }

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AncientBoneShards>()));
    }

    public override void AI()
    {
        if (!TryGetTarget(out Player player))
        {
            Despawn();
            return;
        }

        NPC.timeLeft = NPC.activeTime;
        NPC.ai[1]++;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            EnsureSegments();

        switch (CurrentState)
        {
            case StateAwakening:
                HandleAwakening(player);
                break;
            case StateTransportApproach:
                HandleTransportApproach(player);
                break;
            case StateTransportCarry:
                HandleTransportCarry(player);
                break;
            case StateOrbit:
                HandleOrbit(player);
                break;
            case StatePetrify:
                HandlePetrify(player);
                break;
            case StateDashTelegraph:
                HandleDashTelegraph(player);
                break;
            case StateDash:
                HandleDash(player);
                break;
            case StateCombatGrab:
                HandleCombatGrab(player);
                break;
            case StateBurrow:
                HandleBurrow(player);
                break;
            case StateEmerge:
                HandleEmerge(player);
                break;
            default:
                ServerChangeState(StateOrbit);
                break;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
            HandlePhaseSpawns(player);

        if (CurrentState == StatePetrify)
            NPC.rotation = NPC.rotation.AngleLerp((player.Center - NPC.Center).ToRotation(), 0.14f);
        else if (NPC.velocity.LengthSquared() > 0.01f)
            NPC.rotation = NPC.velocity.ToRotation();
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        return CurrentState is StateDash or StateEmerge;
    }

    public Vector2 GetCarryAnchor()
    {
        Vector2 forward = NPC.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 down = forward.RotatedBy(MathHelper.PiOver2);
        return NPC.Center - forward * 22f + down * 18f;
    }

    private void HandleAwakening(Player player)
    {
        NPC.damage = 0;
        NPC.velocity *= 0.88f;

        int timer = (int)NPC.ai[1];
        if (timer == 1)
        {
            PlayLocalShake(5f, 45);
            BroadcastEncounterText("The earth remembers what should have stayed buried.", new Color(190, 160, 105));
        }
        else if (timer == 70)
        {
            PlayLocalShake(7f, 55);
            BroadcastEncounterText("Ancient bones grind beneath the desert.", new Color(205, 175, 115));
        }
        else if (timer == 140)
        {
            PlayLocalShake(10f, 65);
            BroadcastEncounterText("The slumber of ancient fossils trembles at your presence.", new Color(225, 190, 125));
        }

        if (timer >= 180 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            int next = NPC.ai[3] == SummonModeCarry ? StateTransportApproach : StateOrbit;
            ServerChangeState(next);
        }
    }

    private void HandleTransportApproach(Player player)
    {
        NPC.damage = 0;
        MoveToward(player.Center + new Vector2(0f, -20f), 18f, 0.09f);

        if (Vector2.DistanceSquared(NPC.Center, player.Center) < 100f * 100f &&
            Main.netMode != NetmodeID.MultiplayerClient)
        {
            ServerChangeState(StateTransportCarry);
        }
    }

    private void HandleTransportCarry(Player player)
    {
        NPC.damage = 0;

        Vector2 arenaTarget = FossilEncounterSystem.HasArena
            ? FossilEncounterSystem.ArenaCenter + new Vector2(0f, -120f)
            : player.Center;

        MoveToward(arenaTarget, 30f, 0.10f);

        if (Vector2.DistanceSquared(NPC.Center, arenaTarget) < 170f * 170f &&
            Main.netMode != NetmodeID.MultiplayerClient)
        {
            NPC.ai[3] = SummonModeDirect;
            ServerChangeState(StateOrbit);
        }
    }

    private void HandleOrbit(Player player)
    {
        NPC.damage = 0;
        NPC.alpha = 0;
        NPC.dontTakeDamage = false;

        NPC.localAI[0] += 0.012f;
        float angle = NPC.localAI[0];
        Vector2 orbit = player.Center + new Vector2(
            MathF.Cos(angle) * OrbitRadiusX,
            MathF.Sin(angle) * OrbitRadiusY
        );

        MoveToward(orbit, OrbitSpeed, 0.045f);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (NPC.ai[2] <= 0f)
        {
            NPC.ai[2] = Main.rand.Next(220, 330);
            NPC.netUpdate = true;
        }

        if (NPC.ai[1] < NPC.ai[2])
            return;

        float roll = Main.rand.NextFloat();
        int nextState;

        if (FossilEncounterSystem.LizardSpawnedThisFight && roll < 0.18f)
            nextState = StateBurrow;
        else if (roll < 0.50f)
            nextState = StateDashTelegraph;
        else if (roll < 0.78f)
            nextState = StatePetrify;
        else
            nextState = StateCombatGrab;

        ServerChangeState(nextState);
    }

    private void HandlePetrify(Player player)
    {
        NPC.damage = 0;
        MoveToward(player.Center + new Vector2(-player.direction * 420f, -170f), 7f, 0.055f);

        if ((int)NPC.ai[1] == 1 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item103, NPC.Center);

        if ((int)NPC.ai[1] == 52 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            Vector2 predictedTarget = player.Center + player.velocity * 18f;
            Vector2 direction = (predictedTarget - NPC.Center).SafeNormalize(Vector2.UnitX);

            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                direction * 10f,
                ModContent.ProjectileType<PetrifyShotProjectile>(),
                14,
                1f,
                Main.myPlayer
            );
        }

        if (NPC.ai[1] >= PetrifyTicks && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateOrbit);
    }

    private void HandleDashTelegraph(Player player)
    {
        NPC.damage = 0;

        if ((int)NPC.ai[1] == 1 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            Vector2 direction = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
            _dashStart = NPC.Center;
            _dashEnd = player.Center + direction * 1650f;
            NPC.netUpdate = true;
        }
        
        NPC.localAI[0] += 0.025f;
        Vector2 orbit = player.Center + new Vector2(
            MathF.Cos(NPC.localAI[0]) * OrbitRadiusX,
            MathF.Sin(NPC.localAI[0]) * OrbitRadiusY
        );
        MoveToward(orbit, OrbitSpeed, 0.055f);
        _dashStart = NPC.Center;

        if ((int)NPC.ai[1] == 1)
        {
            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen, NPC.Center);
            PlayLocalShake(7f, 40);
        }

        if (NPC.ai[1] >= DashTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
        {
            _dashStart = NPC.Center;
            Vector2 direction = (player.Center - NPC.Center).SafeNormalize(NPC.velocity.SafeNormalize(Vector2.UnitX));
            _dashEnd = player.Center + direction * 1650f;
            NPC.netUpdate = true;
            ServerChangeState(StateDash);
        }
    }

    private void HandleDash(Player player)
    {
        NPC.damage = 58;
        Vector2 direction = (_dashEnd - _dashStart).SafeNormalize(Vector2.UnitX);
        NPC.velocity = direction * DashSpeed;

        if ((int)NPC.ai[1] == 1)
        {
            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.DD2_BetsyScream, NPC.Center);
            PlayLocalShake(12f, 34);
        }

        if (!Main.dedServ && Main.rand.NextBool(2))
        {
            Dust.NewDustPerfect(
                NPC.Center + Main.rand.NextVector2Circular(40f, 40f),
                DustID.Stone,
                -NPC.velocity * 0.08f + Main.rand.NextVector2Circular(2f, 2f),
                80,
                default,
                1.8f
            );
        }

        bool passedEnd = Vector2.Dot(NPC.Center - _dashEnd, direction) > 0f;
        if ((passedEnd || NPC.ai[1] >= 85f) && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateOrbit);
    }

    private void HandleCombatGrab(Player player)
    {
        NPC.damage = 0;

        if (NPC.ai[1] <= RamTelegraphTicks)
        {
            NPC.localAI[0] += 0.03f;
            Vector2 orbit = player.Center + new Vector2(
                MathF.Cos(NPC.localAI[0]) * OrbitRadiusX,
                MathF.Sin(NPC.localAI[0]) * OrbitRadiusY
            );
            MoveToward(orbit, OrbitSpeed, 0.06f);

            if ((int)NPC.ai[1] == RamTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 chargeDirection = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                _grabDestination = player.Center + player.velocity * 8f + chargeDirection * 1050f;
                NPC.netUpdate = true;
            }
            return;
        }

        Vector2 direction = (_grabDestination - NPC.Center).SafeNormalize(Vector2.UnitX);
        NPC.velocity = direction * 38f;

        if (!_rammedTarget && Vector2.DistanceSquared(NPC.Center, player.Center) < 110f * 110f)
        {
            // The combat pass strikes through the player and sends them toward the arena wall.
            player.velocity = new Vector2(MathF.Sign(NPC.velocity.X) * 26f, -7f);
            player.fallStart = (int)(player.position.Y / 16f);
            _rammedTarget = true;
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NPC.netUpdate = true;
        }

        if (NPC.ai[1] >= 105f && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateOrbit);
    }

    private void HandleBurrow(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = true;
        NPC.alpha = Math.Min(250, NPC.alpha + 12);

        if ((int)NPC.ai[1] == 1 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            _burrowWait = Main.rand.Next(90, 211);
            _burrowTarget = player.Bottom + new Vector2(
                player.velocity.X * 12f + Main.rand.NextFloat(-80f, 80f), 20f);
            NPC.netUpdate = true;
        }

        if (_burrowTarget != Vector2.Zero)
        {
            // Circle beneath the fixed breach point instead of parking there.
            float angle = NPC.ai[1] * 0.045f;
            Vector2 staging = _burrowTarget + new Vector2(
                MathF.Cos(angle) * 125f, 500f + MathF.Sin(angle) * 45f);
            MoveToward(staging, 22f, 0.09f);

            if (!Main.dedServ && _burrowWait > 0 && NPC.ai[1] > _burrowWait - 55)
            {
                Vector2 warning = _burrowTarget + Main.rand.NextVector2Circular(55f, 20f);
                Dust.NewDustPerfect(warning, DustID.Sand,
                    new Vector2(0f, -Main.rand.NextFloat(1f, 4f)), 80, default, 1.5f);
            }
        }

        if (_burrowWait > 0 && NPC.ai[1] >= _burrowWait && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateEmerge);
    }

    private void HandleEmerge(Player player)
    {
        NPC.damage = 48;
        NPC.dontTakeDamage = false;
        NPC.alpha = Math.Max(0, NPC.alpha - 28);

        if ((int)NPC.ai[1] == 1)
        {
            NPC.velocity.Y = -42f;
            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
            PlayLocalShake(10f, 30);
        }
        else
        {
            NPC.velocity.Y += 0.35f;
        }

        NPC.velocity.X = MathHelper.Clamp((_burrowTarget.X - NPC.Center.X) * 0.18f, -12f, 12f);

        if (NPC.ai[1] <= 24f && Vector2.DistanceSquared(NPC.Center, player.Center) < 150f * 150f)
            player.velocity.Y = Math.Min(player.velocity.Y, -14f);

        if (NPC.ai[1] >= 55f && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateOrbit);
    }

    private void HandlePhaseSpawns(Player player)
    {
        if (NPC.life <= NPC.lifeMax * 0.80f)
            FossilEncounterSystem.EnsureLizardSpawned(player);

        if (NPC.life <= NPC.lifeMax * 0.50f)
            FossilEncounterSystem.EnsureSharkSpawned(player);
    }

    private Vector2 ChooseArenaPoint(Vector2 fallback)
    {
        if (!FossilEncounterSystem.HasArena)
            return fallback + Main.rand.NextVector2Circular(420f, 260f);

        Rectangle arena = FossilEncounterSystem.ArenaBounds;
        float x = Main.rand.NextFloat(arena.Left + 180f, arena.Right - 180f);
        float y = Main.rand.NextFloat(arena.Top + 180f, arena.Bottom - 180f);
        return new Vector2(x, y);
    }

    private void EnsureSegments()
    {
        int bodyType = ModContent.NPCType<FossilSnakeBody>();
        int tailType = ModContent.NPCType<FossilSnakeTail>();
        int[] segments = _segmentSlots;
        Array.Fill(segments, -1);
        segments[0] = NPC.whoAmI;

        for (int slot = 0; slot < Main.maxNPCs; slot++)
        {
            NPC candidate = Main.npc[slot];
            if (!candidate.active || (int)candidate.ai[2] != NPC.whoAmI)
                continue;

            int segmentNumber = (int)candidate.ai[1];
            if (segmentNumber < 1 || segmentNumber >= SegmentCount)
                continue;

            int expectedType = segmentNumber == SegmentCount - 1 ? tailType : bodyType;
            if (candidate.type == expectedType && segments[segmentNumber] < 0)
                segments[segmentNumber] = slot;
        }

        Vector2 backward = -NPC.velocity.SafeNormalize(Vector2.UnitX);
        for (int i = 1; i < SegmentCount; i++)
        {
            int previous = segments[i - 1];
            if (previous < 0)
                break;

            float spacing = i == 1 ? HeadToBodySpacing : i > 42 ? TailSpacing : SegmentSpacing;
            int index = segments[i];
            if (index < 0)
            {
                Vector2 spawnPosition = Main.npc[previous].Center + backward * spacing;
                index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawnPosition.X, (int)spawnPosition.Y,
                    i == SegmentCount - 1 ? tailType : bodyType, Start: 0, ai0: previous,
                    ai1: i, ai2: NPC.whoAmI, Target: NPC.target);

                if (index < 0 || index >= Main.maxNPCs)
                    break;

                segments[i] = index;
            }

            NPC segment = Main.npc[index];
            if ((int)segment.ai[0] != previous || segment.realLife != NPC.whoAmI)
            {
                segment.ai[0] = previous;
                segment.realLife = NPC.whoAmI;
                segment.netUpdate = true;
            }
        }
    }

    private bool TryGetTarget(out Player player)
    {
        if (NPC.target < 0 || NPC.target >= Main.maxPlayers)
            NPC.TargetClosest(false);

        player = Main.player[NPC.target];
        if (player.active && !player.dead)
            return true;

        NPC.TargetClosest(false);
        player = Main.player[NPC.target];
        return player.active && !player.dead;
    }

    private void Despawn()
    {
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.velocity.Y += 0.5f;
        if (NPC.timeLeft > 30)
            NPC.timeLeft = 30;
    }

    private void ServerChangeState(int state)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        NPC.ai[0] = state;
        NPC.ai[1] = 0f;
        NPC.ai[2] = 0f;

        if (state == StateCombatGrab)
            _rammedTarget = false;
        if (state == StateBurrow)
        {
            _burrowTarget = Vector2.Zero;
            _burrowWait = 0;
        }

        NPC.netUpdate = true;
    }

    private void MoveToward(Vector2 target, float speed, float acceleration)
    {
        Vector2 desired = (target - NPC.Center).SafeNormalize(Vector2.Zero) * speed;
        NPC.velocity = Vector2.Lerp(NPC.velocity, desired, acceleration);
    }

    private void PlayLocalShake(float strength, int frames)
    {
        if (Main.dedServ || Main.LocalPlayer is null || !Main.LocalPlayer.active)
            return;

        if (Vector2.DistanceSquared(Main.LocalPlayer.Center, NPC.Center) > 2600f * 2600f)
            return;

        Vector2 direction = (Main.LocalPlayer.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
        Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, direction, strength, 5f, frames));
    }

    private static void BroadcastEncounterText(string text, Color color)
    {
        if (Main.netMode == NetmodeID.Server)
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
        else if (Main.netMode == NetmodeID.SinglePlayer)
            Main.NewText(text, color);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (CurrentState == StateDashTelegraph && _dashStart != Vector2.Zero && _dashEnd != Vector2.Zero)
        {
            float pulse = 0.35f + 0.45f * MathF.Abs(MathF.Sin(Main.GlobalTimeWrappedHourly * 9f));
            Utils.DrawLine(
                spriteBatch,
                _dashStart - screenPos,
                _dashEnd - screenPos,
                Color.OrangeRed * pulse,
                Color.Gold * pulse,
                5f
            );
        }

        if (CurrentState == StateBurrow && _burrowTarget != Vector2.Zero &&
            _burrowWait > 0 && NPC.ai[1] >= _burrowWait - 120f)
        {
            float pulse = 0.7f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f);
            Utils.DrawBorderString(spriteBatch, "!",
                _burrowTarget - screenPos + new Vector2(0f, -95f),
                Color.Yellow * pulse, 1.8f, 0.5f, 0.5f);
        }

        Texture2D head = ModContent.Request<Texture2D>(SpriteRoot + "SnakeSkull", AssetRequestMode.ImmediateLoad).Value;
        Texture2D jaw = ModContent.Request<Texture2D>(SpriteRoot + "SnakeJaw", AssetRequestMode.ImmediateLoad).Value;

        Vector2 drawPosition = NPC.Center - screenPos;
        float rotation = NPC.rotation + MathHelper.PiOver2;
        SpriteEffects effects = SpriteEffects.None;

        spriteBatch.Draw(
            head,
            drawPosition,
            null,
            drawColor,
            rotation,
            head.Size() * 0.5f,
            1f,
            effects,
            0f
        );

        // The jaw opens through the windup, reaches its widest point as the
        // homing bone is fired on tick 52, then closes again.
        float jawOpen = CurrentState == StatePetrify
            ? MathHelper.Clamp((NPC.ai[1] - 30f) / 22f, 0f, 1f) *
              MathHelper.Clamp((75f - NPC.ai[1]) / 23f, 0f, 1f) * 0.52f
            : CurrentState == StateCombatGrab ? 0.12f : 0f;
        // In the supplied right-facing reference s2 begins two pixels right and 38 pixels
        // below s1, putting its center 12 pixels behind and 29 pixels below the skull center.
        Vector2 jawOffset = new Vector2(-12f, 29f).RotatedBy(NPC.rotation);

        Vector2 jawPivot = new(10f, 100f);
        Vector2 jawPosition = drawPosition + jawOffset +
            (jawPivot - jaw.Size() * 0.5f).RotatedBy(rotation);
        spriteBatch.Draw(jaw, jawPosition, null, drawColor,
            rotation + jawOpen, jawPivot, 1f, effects, 0f);

        return false;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(_dashStart.X);
        writer.Write(_dashStart.Y);
        writer.Write(_dashEnd.X);
        writer.Write(_dashEnd.Y);
        writer.Write(_grabDestination.X);
        writer.Write(_grabDestination.Y);
        writer.Write(_burrowTarget.X);
        writer.Write(_burrowTarget.Y);
        writer.Write(_rammedTarget);
        writer.Write(_burrowWait);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        _dashStart = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        _dashEnd = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        _grabDestination = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        _burrowTarget = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        _rammedTarget = reader.ReadBoolean();
        _burrowWait = reader.ReadInt32();
    }
}

public sealed class FossilSnakeBody : ModNPC
{
    private const string SpriteRoot = "Blasphemy/Assets/Textures/Bosses/Fossil/Sprites/";
    private const float SegmentSpacing = 29f;

    public override string Texture => SpriteRoot + "SnakeNeckRib";

    public override void SetDefaults()
    {
        NPC.width = 42;
        NPC.height = 32;
        NPC.damage = 46;
        NPC.defense = 9999;
        NPC.lifeMax = 1;
        NPC.aiStyle = -1;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.lavaImmune = true;
        NPC.trapImmune = true;
        NPC.knockBackResist = 0f;
        NPC.behindTiles = true;
        NPC.dontTakeDamage = true;
        NPC.npcSlots = 0f;
    }

    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;

        int previousIndex = (int)NPC.ai[0];
        int headIndex = (int)NPC.ai[2];
        if (!IsValid(headIndex) || Main.npc[headIndex].ModNPC is not FossilSnakeHead)
        {
            NPC.active = false;
            return;
        }

        NPC.realLife = headIndex;
        int segmentNumber = (int)NPC.ai[1];
        bool linked = IsValid(previousIndex) && (segmentNumber == 1
            ? previousIndex == headIndex
            : Main.npc[previousIndex].ModNPC is FossilSnakeBody &&
              (int)Main.npc[previousIndex].ai[1] == segmentNumber - 1 &&
              (int)Main.npc[previousIndex].ai[2] == headIndex);
        Follow(linked ? Main.npc[previousIndex] : Main.npc[headIndex], !linked);
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        int headIndex = (int)NPC.ai[2];
        if (!IsValid(headIndex) || Main.npc[headIndex].ModNPC is not FossilSnakeHead head)
            return false;

        return head.CurrentState is FossilSnakeHead.StateDash or FossilSnakeHead.StateEmerge;
    }

    private void Follow(NPC previous, bool headFallback)
    {
        Vector2 toPrevious = previous.Center - NPC.Center;
        float distance = toPrevious.Length();
        if (distance < 0.001f)
            return;

        Vector2 direction = toPrevious / distance;
        int segmentNumber = (int)NPC.ai[1];
        float spacing = segmentNumber == 1 ? 53f : segmentNumber > 42 ? 25f : SegmentSpacing;
        if (headFallback)
            spacing = 53f + Math.Min(segmentNumber - 1, 41) * SegmentSpacing + Math.Max(0, segmentNumber - 42) * 25f;
        NPC.Center = previous.Center - direction * spacing;
        NPC.velocity = Vector2.Zero;
        NPC.rotation = direction.ToRotation();
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        int segmentIndex = (int)NPC.ai[1];
        string texturePath = segmentIndex <= 8 ? SpriteRoot + "SnakeNeckRib" :
            segmentIndex <= 42 ? SpriteRoot + "SnakeBodyRib" : SpriteRoot + "SnakeTailRib";

        Texture2D texture = ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad).Value;
        spriteBatch.Draw(
            texture,
            NPC.Center - screenPos,
            null,
            drawColor,
            NPC.rotation + MathHelper.PiOver2,
            texture.Size() * 0.5f,
            1f,
            SpriteEffects.None,
            0f
        );

        return false;
    }

    private static bool IsValid(int index) => index >= 0 && index < Main.maxNPCs && Main.npc[index].active;
}

public sealed class FossilSnakeTail : ModNPC
{
    private const string SpriteRoot = "Blasphemy/Assets/Textures/Bosses/Fossil/Sprites/";
    private const float SegmentSpacing = 25f;

    public override string Texture => SpriteRoot + "SnakeTailBase";

    public override void SetDefaults()
    {
        NPC.width = 30;
        NPC.height = 26;
        NPC.damage = 38;
        NPC.defense = 9999;
        NPC.lifeMax = 1;
        NPC.aiStyle = -1;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.lavaImmune = true;
        NPC.trapImmune = true;
        NPC.knockBackResist = 0f;
        NPC.behindTiles = true;
        NPC.dontTakeDamage = true;
        NPC.npcSlots = 0f;
    }

    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;

        int previousIndex = (int)NPC.ai[0];
        int headIndex = (int)NPC.ai[2];
        if (!IsValid(headIndex) || Main.npc[headIndex].ModNPC is not FossilSnakeHead)
        {
            NPC.active = false;
            return;
        }

        NPC.realLife = headIndex;
        bool linked = IsValid(previousIndex) && Main.npc[previousIndex].ModNPC is FossilSnakeBody &&
            (int)Main.npc[previousIndex].ai[1] == (int)NPC.ai[1] - 1 &&
            (int)Main.npc[previousIndex].ai[2] == headIndex;
        NPC previous = linked ? Main.npc[previousIndex] : Main.npc[headIndex];
        Vector2 toPrevious = previous.Center - NPC.Center;
        float distance = toPrevious.Length();
        if (distance > 0.001f)
        {
            Vector2 direction = toPrevious / distance;
            float spacing = linked ? SegmentSpacing : 53f + 41f * 29f + 17f * SegmentSpacing;
            NPC.Center = previous.Center - direction * spacing;
            NPC.rotation = direction.ToRotation();
        }

        NPC.velocity = Vector2.Zero;
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        int headIndex = (int)NPC.ai[2];
        if (!IsValid(headIndex) || Main.npc[headIndex].ModNPC is not FossilSnakeHead head)
            return false;

        return head.CurrentState is FossilSnakeHead.StateDash or FossilSnakeHead.StateEmerge;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D tailBase = ModContent.Request<Texture2D>(SpriteRoot + "SnakeTailBase", AssetRequestMode.ImmediateLoad).Value;
        Texture2D tailTip = ModContent.Request<Texture2D>(SpriteRoot + "SnakeTailTip", AssetRequestMode.ImmediateLoad).Value;

        Vector2 drawPos = NPC.Center - screenPos;
        float rotation = NPC.rotation + MathHelper.PiOver2;
        Vector2 tipOffset = new Vector2(-34f, 5f).RotatedBy(NPC.rotation);

        spriteBatch.Draw(tailBase, drawPos, null, drawColor, rotation, tailBase.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
        spriteBatch.Draw(tailTip, drawPos + tipOffset, null, drawColor, rotation, tailTip.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
        return false;
    }

    private static bool IsValid(int index) => index >= 0 && index < Main.maxNPCs && Main.npc[index].active;
}
