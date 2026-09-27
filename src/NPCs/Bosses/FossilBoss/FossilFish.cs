using System;
using System.IO;
using Blasphemy.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.NPCs.Bosses.FossilBoss;

[AutoloadBossHead]
public sealed class FossilFish : ModNPC
{
    private const string SpriteRoot = "Blasphemy/Assets/Textures/Bosses/Fossil/Sprites/";

    private const int StateUnderSand = 0;
    private const int StateJumpTelegraph = 1;
    private const int StateJump = 2;
    private const int StateDive = 3;
    private const int StateFinalPatrol = 4;
    private const int StateFinalDashTelegraph = 5;
    private const int StateFinalDash = 6;
    private const int StateFinalRecover = 7;
    private const int StateFinalReposition = 8;

    private const int JumpTelegraphTicks = 2 * 60;
    private const int FinalDashTelegraphTicks = 22;
    private const int FinalDashCount = 5;
    private const float SpriteScale = 2f / 3f;

    private Vector2 _jumpPoint;
    private Vector2 _dashStart;
    private Vector2 _dashEnd;
    private int _completedFinalDashes;

    public bool IsInFinalSoloPhase => FossilEncounterSystem.IsSharkAlone();

    public override string Texture => SpriteRoot + "SharkRibCage";
    public override string BossHeadTexture => "Blasphemy/Assets/Textures/Bosses/Fossil/SharkHead";

    public override void SetStaticDefaults()
    {
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.MustAlwaysDraw[Type] = true;
        NPCID.Sets.BossBestiaryPriority.Add(Type);
    }

    public override void SetDefaults()
    {
        NPC.width = 200;
        NPC.height = 100;
        NPC.damage = 46;
        NPC.defense = 8;
        NPC.lifeMax = 3600;
        NPC.aiStyle = -1;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.lavaImmune = true;
        NPC.trapImmune = true;
        NPC.knockBackResist = 0f;
        NPC.behindTiles = true;
        NPC.boss = true;
        NPC.npcSlots = 200f;
        NPC.BossBar = ModContent.GetInstance<FossilSharkBossBar>();
        NPC.netAlways = true;
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

        if (Main.netMode != NetmodeID.MultiplayerClient && FossilEncounterSystem.IsSharkAlone() && (int)NPC.ai[0] < StateFinalPatrol)
        {
            _completedFinalDashes = 0;
            ServerChangeState(StateFinalPatrol);
        }

        switch ((int)NPC.ai[0])
        {
            case StateUnderSand:
                HandleUnderSand(player);
                break;
            case StateJumpTelegraph:
                HandleJumpTelegraph(player);
                break;
            case StateJump:
                HandleJump(player);
                break;
            case StateDive:
                HandleDive(player);
                break;
            case StateFinalPatrol:
                HandleFinalPatrol(player);
                break;
            case StateFinalDashTelegraph:
                HandleFinalDashTelegraph(player);
                break;
            case StateFinalDash:
                HandleFinalDash(player);
                break;
            case StateFinalRecover:
                HandleFinalRecover(player);
                break;
            case StateFinalReposition:
                HandleFinalReposition(player);
                break;
            default:
                ServerChangeState(StateUnderSand);
                break;
        }

        if (NPC.velocity.LengthSquared() > 0.01f)
            NPC.rotation = NPC.velocity.ToRotation();
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        int state = (int)NPC.ai[0];
        return state is StateJump or StateFinalDash;
    }

    private void HandleUnderSand(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = true;
        NPC.behindTiles = true;
        NPC.alpha = 80;

        float surfaceY = FindSurfaceY(player);
        NPC.ai[2] = surfaceY;

        NPC.localAI[0] += 0.018f;
        Vector2 target = new Vector2(
            player.Center.X + MathF.Sin(NPC.localAI[0]) * 420f,
            surfaceY + 135f + MathF.Sin(NPC.localAI[0] * 1.7f) * 35f
        );
        MoveToward(target, 8f, 0.06f);
        SpawnSand(player, surfaceY, dense: false);

        if (NPC.ai[1] >= 210f && Main.netMode != NetmodeID.MultiplayerClient)
        {
            _jumpPoint = new Vector2(
                player.Center.X + player.velocity.X * 14f + Main.rand.NextFloat(-75f, 75f),
                surfaceY - 10f
            );
            NPC.netUpdate = true;
            ServerChangeState(StateJumpTelegraph);
        }
    }

    private void HandleJumpTelegraph(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = true;
        NPC.behindTiles = true;
        NPC.alpha = 100;

        float surfaceY = FindSurfaceY(player);
        NPC.ai[2] = surfaceY;
        _jumpPoint.Y = surfaceY - 10f;
        Vector2 hiddenTarget = new Vector2(_jumpPoint.X, surfaceY + 330f);
        MoveToward(hiddenTarget, 14f, 0.12f);
        SpawnSandVortex(_jumpPoint, MathHelper.Clamp(NPC.ai[1] / JumpTelegraphTicks, 0f, 1f));

        if ((int)NPC.ai[1] == 1 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.DD2_OgreRoar, _jumpPoint);

        if (NPC.ai[1] >= JumpTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateJump);
    }

    private void HandleJump(Player player)
    {
        NPC.damage = 52;
        NPC.dontTakeDamage = false;
        NPC.behindTiles = false;
        NPC.alpha = Math.Max(0, NPC.alpha - 18);

        if ((int)NPC.ai[1] == 1)
        {
            NPC.Center = new Vector2(_jumpPoint.X, (NPC.ai[2] > 0f ? NPC.ai[2] : player.Center.Y) + 300f);
            float horizontal = MathHelper.Clamp((player.Center.X - NPC.Center.X) * 0.025f, -11f, 11f);
            NPC.velocity = new Vector2(horizontal, -28f);

            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.NPCDeath19, NPC.Center);
            SpawnSandBurst(_jumpPoint);
        }
        else
        {
            NPC.velocity.Y += 0.85f;
        }

        float surfaceY = FindSurfaceY(player);
        if (NPC.ai[1] > 30f && NPC.Center.Y > surfaceY + 180f && NPC.velocity.Y > 0f && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateDive);
    }

    private void HandleDive(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = true;
        NPC.behindTiles = true;
        NPC.alpha = Math.Min(110, NPC.alpha + 12);

        float surfaceY = FindSurfaceY(player);
        Vector2 target = new Vector2(player.Center.X, surfaceY + 180f);
        MoveToward(target, 16f, 0.10f);
        SpawnSand(player, surfaceY, dense: true);

        if (NPC.ai[1] >= 55f && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateUnderSand);
    }

    private void HandleFinalPatrol(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = false;
        NPC.behindTiles = false;
        NPC.alpha = 0;

        NPC.localAI[0] += 0.025f;
        Vector2 target = player.Center + new Vector2(
            MathF.Cos(NPC.localAI[0]) * 600f,
            MathF.Sin(NPC.localAI[0] * 1.25f) * 300f
        );
        MoveToward(target, 12f, 0.075f);
        SpawnSand(player, player.Center.Y + 150f, dense: true);

        if (NPC.ai[1] >= 95f && Main.netMode != NetmodeID.MultiplayerClient)
        {
            _completedFinalDashes = 0;
            ServerChangeState(StateFinalReposition);
        }
    }

    private void HandleFinalDashTelegraph(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = false;
        NPC.behindTiles = false;
        NPC.alpha = Math.Max(0, NPC.alpha - 16);
        NPC.velocity *= 0.65f;

        if ((int)NPC.ai[1] == 1 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item71, NPC.Center);

        if (NPC.ai[1] >= FinalDashTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateFinalDash);
    }

    private void HandleFinalDash(Player player)
    {
        NPC.damage = 58;
        NPC.dontTakeDamage = false;
        NPC.behindTiles = false;
        NPC.alpha = 0;

        Vector2 direction = (_dashEnd - _dashStart).SafeNormalize(Vector2.UnitX);
        NPC.velocity = direction * 36f;

        bool passedEnd = Vector2.Dot(NPC.Center - _dashEnd, direction) > 0f;
        if ((passedEnd || NPC.ai[1] >= 48f) && Main.netMode != NetmodeID.MultiplayerClient)
        {
            _completedFinalDashes++;

            if (_completedFinalDashes >= FinalDashCount)
            {
                ServerChangeState(StateFinalRecover);
            }
            else
            {
                ServerChangeState(StateFinalReposition);
            }
        }
    }

    private void HandleFinalReposition(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = true;
        NPC.behindTiles = true;
        NPC.alpha = Math.Min(255, NPC.alpha + 12);

        if ((int)NPC.ai[1] == 1)
        {
            Vector2 outward = (NPC.Center - player.Center).SafeNormalize(NPC.velocity.SafeNormalize(Vector2.UnitX));
            NPC.ai[3] = outward.ToRotation();
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NPC.netUpdate = true;
        }

        NPC.velocity = NPC.ai[3].ToRotationVector2() * 27f;

        // Leave the player's view before changing position for the next charge.
        float offscreenRadius = Math.Max(1900f, Math.Max(Main.screenWidth, Main.screenHeight) * 0.7f + 350f);
        if (NPC.ai[1] >= 36f && Vector2.DistanceSquared(NPC.Center, player.Center) >= offscreenRadius * offscreenRadius &&
            Main.netMode != NetmodeID.MultiplayerClient)
        {
            PrepareFinalDash(player);
            ServerChangeState(StateFinalDashTelegraph);
        }
    }

    private void HandleFinalRecover(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = false;
        NPC.velocity *= 0.94f;
        SpawnSand(player, player.Center.Y + 150f, dense: true);

        if (NPC.ai[1] >= 75f && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateFinalPatrol);
    }

    private void PrepareFinalDash(Player player)
    {
        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
        Vector2 direction = angle.ToRotationVector2();
        Vector2 sideOffset = direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-240f, 240f);
        Vector2 predicted = player.Center + player.velocity * Main.rand.NextFloat(8f, 20f);

        _dashStart = predicted - direction * 1500f + sideOffset;
        _dashEnd = predicted + direction * 1500f + sideOffset;
        NPC.Center = _dashStart;
        NPC.velocity = Vector2.Zero;
        NPC.netUpdate = true;
    }

    private static float FindSurfaceY(Player player) => player.Bottom.Y + 48f;

    private void SpawnSand(Player player, float surfaceY, bool dense)
    {
        if (Main.dedServ)
            return;

        int count = dense ? 5 : 2;
        for (int i = 0; i < count; i++)
        {
            Vector2 position = new Vector2(
                NPC.Center.X + Main.rand.NextFloat(-180f, 180f),
                surfaceY + Main.rand.NextFloat(-30f, 150f)
            );

            Dust.NewDustPerfect(
                position,
                DustID.Sandstorm,
                new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, 0.5f)),
                130,
                default,
                dense ? 1.6f : 1.2f
            );
        }
    }

    private static void SpawnSandVortex(Vector2 center, float progress)
    {
        if (Main.dedServ)
            return;

        int count = 3 + (int)(progress * 4f);
        float time = Main.GlobalTimeWrappedHourly * 12f;
        for (int i = 0; i < count; i++)
        {
            float height = Main.rand.NextFloat(8f, 160f);
            float angle = time + i * MathHelper.TwoPi / count + height * 0.025f;
            float radius = 30f + height * 0.28f + Main.rand.NextFloat(-9f, 9f);
            Vector2 position = center + new Vector2(MathF.Cos(angle) * radius, -height);
            Vector2 velocity = new Vector2(-MathF.Sin(angle) * 3.8f, -Main.rand.NextFloat(1.5f, 3.5f));
            Dust dust = Dust.NewDustPerfect(position, i % 3 == 0 ? DustID.Sandstorm : DustID.Sandnado,
                velocity, 80, default, Main.rand.NextFloat(1.1f, 1.7f));
            dust.noGravity = true;
        }
    }

    private static void SpawnSandBurst(Vector2 center)
    {
        if (Main.dedServ)
            return;

        for (int i = 0; i < 36; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(8f, 3f) + new Vector2(0f, -Main.rand.NextFloat(2f, 7f));
            Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(55f, 12f),
                DustID.Sandstorm, velocity, 80, default, Main.rand.NextFloat(1.2f, 1.9f));
            dust.noGravity = true;
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
        NPC.netUpdate = true;
    }

    private void MoveToward(Vector2 target, float speed, float acceleration)
    {
        Vector2 desired = (target - NPC.Center).SafeNormalize(Vector2.Zero) * speed;
        NPC.velocity = Vector2.Lerp(NPC.velocity, desired, acceleration);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        int state = (int)NPC.ai[0];

        if (state == StateJumpTelegraph && _jumpPoint != Vector2.Zero)
        {
            float pulse = 0.7f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f);
            Utils.DrawBorderString(
                spriteBatch,
                "!",
                _jumpPoint - screenPos + new Vector2(0f, -95f),
                Color.Yellow * pulse,
                1.8f,
                0.5f,
                0.5f
            );
        }

        if (state == StateFinalDashTelegraph && _dashStart != Vector2.Zero && _dashEnd != Vector2.Zero)
        {
            float pulse = 0.2f + 0.25f * MathF.Abs(MathF.Sin(Main.GlobalTimeWrappedHourly * 16f));
            Utils.DrawLine(spriteBatch, _dashStart - screenPos, _dashEnd - screenPos, Color.SandyBrown * pulse, Color.Gray * pulse, 3f);
        }

        if (state is StateUnderSand or StateJumpTelegraph or StateDive)
            return false;

        Color color = IsInFinalSoloPhase ? new Color(145, 145, 145) : drawColor;
        color *= 1f - NPC.alpha / 255f;
        DrawComposite(spriteBatch, NPC.Center - screenPos, NPC.rotation, color, state, NPC.ai[1]);
        return false;
    }

    private static void DrawComposite(SpriteBatch spriteBatch, Vector2 center, float rotation, Color color, int state, float stateTime)
    {
        Texture2D head = ModContent.Request<Texture2D>(SpriteRoot + "SharkSkull", AssetRequestMode.ImmediateLoad).Value;
        Texture2D jaw = ModContent.Request<Texture2D>(SpriteRoot + "SharkJaw", AssetRequestMode.ImmediateLoad).Value;
        Texture2D ribs = ModContent.Request<Texture2D>(SpriteRoot + "SharkRibCage", AssetRequestMode.ImmediateLoad).Value;
        Texture2D lowerFin = ModContent.Request<Texture2D>(SpriteRoot + "SharkPectoralFin", AssetRequestMode.ImmediateLoad).Value;
        Texture2D spine = ModContent.Request<Texture2D>(SpriteRoot + "SharkSpine", AssetRequestMode.ImmediateLoad).Value;
        Texture2D tail = ModContent.Request<Texture2D>(SpriteRoot + "SharkTailFin", AssetRequestMode.ImmediateLoad).Value;

        bool facesLeft = MathF.Cos(rotation) < 0f;
        SpriteEffects effects = facesLeft ? SpriteEffects.FlipVertically : SpriteEffects.None;
        float facing = facesLeft ? -1f : 1f;
        float activity = state switch
        {
            StateJump => 1f,
            StateFinalDash => 0.9f,
            StateFinalPatrol => 0.55f,
            _ => 0.35f
        };
        float swim = Main.GameUpdateCount * 0.22f;
        center += new Vector2(0f, MathF.Sin(swim) * 2f * activity);
        float tailSwing = MathF.Sin(swim) * 0.18f * activity * facing;
        float finSwing = MathF.Sin(swim + 1.3f) * 0.22f * activity * facing;
        float headNod = MathF.Sin(swim + 0.7f) * 0.035f * activity * facing;

        // Bite during the breach, then close the mouth as the shark falls back into the sand.
        float bite = state == StateJump
            ? MathHelper.Clamp((stateTime - 2f) / 10f, 0f, 1f) *
              MathHelper.Clamp((55f - stateTime) / 18f, 0f, 1f)
            : state == StateFinalDash
                ? MathHelper.Clamp(stateTime / 8f, 0f, 1f) * 0.8f
                : 0f;

        DrawHingedPart(spriteBatch, tail, center, new Vector2(-301f, 54f),
            new Vector2(105f, 68f), rotation, tailSwing, color, effects);
        DrawPart(spriteBatch, spine, center, new Vector2(-205f, -4f), rotation, color, effects, 1f);
        DrawPart(spriteBatch, ribs, center, Vector2.Zero, rotation, color, effects, 1f);
        DrawHingedPart(spriteBatch, lowerFin, center, new Vector2(-11f, 106f),
            new Vector2(155f, 18f), rotation, finSwing, color, effects);
        DrawPart(spriteBatch, head, center, new Vector2(222f, 20f), rotation + headNod, color, effects, 1f);
        DrawHingedPart(spriteBatch, jaw, center, new Vector2(218f, 101f),
            new Vector2(22f, 22f), rotation + headNod, (0.04f + bite * 0.42f) * facing,
            color, effects);
    }

    private static void DrawHingedPart(
        SpriteBatch spriteBatch, Texture2D texture, Vector2 center, Vector2 localOffset,
        Vector2 pivot, float rotation, float hingeRotation, Color color, SpriteEffects effects)
    {
        if ((effects & SpriteEffects.FlipVertically) != 0)
        {
            localOffset.Y *= -1f;
            pivot.Y = texture.Height - pivot.Y;
        }
        
        Vector2 pivotOffset = localOffset + pivot - texture.Size() * 0.5f;
        Vector2 pivotWorld = center + (pivotOffset * SpriteScale).RotatedBy(rotation);
        spriteBatch.Draw(texture, pivotWorld, null, color, rotation + hingeRotation,
            pivot, SpriteScale, effects, 0f);
    }

    private static void DrawPart(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Vector2 center,
        Vector2 localOffset,
        float rotation,
        Color color,
        SpriteEffects effects,
        float scale)
    {
        if ((effects & SpriteEffects.FlipVertically) != 0)
            localOffset.Y *= -1f;

        Vector2 position = center + (localOffset * SpriteScale).RotatedBy(rotation);
        spriteBatch.Draw(texture, position, null, color, rotation, texture.Size() * 0.5f,
            scale * SpriteScale, effects, 0f);
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        WriteVector(writer, _jumpPoint);
        WriteVector(writer, _dashStart);
        WriteVector(writer, _dashEnd);
        writer.Write(_completedFinalDashes);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        _jumpPoint = ReadVector(reader);
        _dashStart = ReadVector(reader);
        _dashEnd = ReadVector(reader);
        _completedFinalDashes = reader.ReadInt32();
    }

    private static void WriteVector(BinaryWriter writer, Vector2 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    private static Vector2 ReadVector(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle());
}
