using System;
using System.IO;
using Blasphemy.Projectiles;
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
public sealed class FossilLizard : ModNPC
{
    private const string SpriteRoot = "Blasphemy/Assets/Textures/Bosses/Fossil/Sprites/";

    private const int StateBuried = 0;
    private const int StateEmerging = 1;
    private const int StateSpit = 2;
    private const int StateHypnosis = 3;
    private const int StateRetreat = 4;

    private const int HypnosisTellTicks = 4 * 60;
    private const int HypnosisHoldTicks = 10 * 60;
    private const float FlareScale = 1f;

    private static Texture2D _flareDrawTexture;
    private static bool _ownsFlareDrawTexture;

    private Vector2 _buriedPoint;
    private Vector2 _emergePoint;
    private Vector2 _surfaceNormal = Vector2.UnitY;

    public bool IsHypnosisState => (int)NPC.ai[0] == StateHypnosis;
    public Vector2 FlareWorldPosition
    {
        get
        {
            Vector2 eyeOffset = new(-30f, -30f);
            if (MathF.Cos(NPC.rotation) < 0f)
                eyeOffset.Y *= -1f;
            return NPC.Center + eyeOffset.RotatedBy(NPC.rotation);
        }
    }

    public override string Texture => SpriteRoot + "LizardSkull";
    public override string BossHeadTexture => "Blasphemy/Assets/Textures/Bosses/Fossil/LizardHead";

    public override void Unload()
    {
        if (_ownsFlareDrawTexture)
            _flareDrawTexture?.Dispose();

        _flareDrawTexture = null;
        _ownsFlareDrawTexture = false;
    }

    public override void SetStaticDefaults()
    {
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.MustAlwaysDraw[Type] = true;
        NPCID.Sets.BossBestiaryPriority.Add(Type);
    }

    public override void SetDefaults()
    {
        NPC.width = 118;
        NPC.height = 72;
        NPC.damage = 0;
        NPC.defense = 8;
        NPC.lifeMax = 2900;
        NPC.aiStyle = -1;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.lavaImmune = true;
        NPC.trapImmune = true;
        NPC.knockBackResist = 0f;
        NPC.behindTiles = true;
        NPC.boss = true;
        NPC.npcSlots = 200f;
        NPC.BossBar = ModContent.GetInstance<FossilLizardBossBar>();
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

        switch ((int)NPC.ai[0])
        {
            case StateBuried:
                HandleBuried(player);
                break;
            case StateEmerging:
                HandleEmerging(player);
                break;
            case StateSpit:
                HandleSpit(player);
                break;
            case StateHypnosis:
                HandleHypnosis(player);
                break;
            case StateRetreat:
                HandleRetreat(player);
                break;
            default:
                ServerChangeState(StateBuried);
                break;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient && NPC.life <= NPC.lifeMax * 0.50f)
            FossilEncounterSystem.EnsureSharkSpawned(player);

        if (!Main.dedServ && IsHypnosisState)
            Lighting.AddLight(FlareWorldPosition, 1.2f, 0.85f, 0.25f);
    }

    private void HandleBuried(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = true;
        NPC.behindTiles = true;
        NPC.alpha = 255;
        NPC.velocity *= 0.92f;

        if ((int)NPC.ai[1] == 1 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            FindEmergencePoint(player);
            NPC.Center = _buriedPoint;

            // ai[2] is the attack selected for this appearance: 0 = spit, 1 = hypnosis.
            NPC.ai[2] = Main.rand.NextFloat() < 0.3f ? 1f : 0f;
            NPC.netUpdate = true;
        }

        if (NPC.ai[1] >= 40f && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateEmerging);
    }

    private void HandleEmerging(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = false;
        NPC.behindTiles = false;
        NPC.alpha = Math.Max(0, NPC.alpha - 12);

        Vector2 desired = (_emergePoint - NPC.Center).SafeNormalize(Vector2.Zero) * 12f;
        NPC.velocity = Vector2.Lerp(NPC.velocity, desired, 0.18f);
        NPC.rotation = (player.Center - NPC.Center).ToRotation();

        if (Vector2.DistanceSquared(NPC.Center, _emergePoint) < 28f * 28f || NPC.ai[1] >= 55f)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                ServerChangeState(NPC.ai[2] >= 0.5f ? StateHypnosis : StateSpit);
        }
    }

    private void HandleSpit(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = false;
        NPC.behindTiles = false;
        HoverNearEmergence();
        NPC.rotation = (player.Center - NPC.Center).ToRotation();

        int timer = (int)NPC.ai[1];
        if (timer is 35 or 70 or 105)
        {
            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item17, NPC.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient)
                FireWeakHomingVolley(player);
        }

        if (timer >= 145 && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateRetreat);
    }

    private void HandleHypnosis(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = false;
        NPC.behindTiles = false;
        HoverNearEmergence();
        NPC.rotation = (player.Center - NPC.Center).ToRotation();

        if ((int)NPC.ai[1] == 1 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item29, NPC.Center);

        if (NPC.justHit && Main.netMode != NetmodeID.MultiplayerClient)
        {
            ServerChangeState(StateRetreat);
            return;
        }

        if (NPC.ai[1] >= HypnosisTellTicks + HypnosisHoldTicks && Main.netMode != NetmodeID.MultiplayerClient)
            ServerChangeState(StateRetreat);
    }

    private void HandleRetreat(Player player)
    {
        NPC.damage = 0;
        NPC.dontTakeDamage = true;
        NPC.behindTiles = true;
        NPC.alpha = Math.Min(255, NPC.alpha + 16);

        Vector2 desired = (_buriedPoint - NPC.Center).SafeNormalize(Vector2.Zero) * 15f;
        NPC.velocity = Vector2.Lerp(NPC.velocity, desired, 0.18f);

        if (Vector2.DistanceSquared(NPC.Center, _buriedPoint) < 35f * 35f || NPC.ai[1] >= 60f)
        {
            NPC.Center = _buriedPoint;
            NPC.velocity = Vector2.Zero;

            if (Main.netMode != NetmodeID.MultiplayerClient)
                ServerChangeState(StateBuried);
        }
    }

    private void HoverNearEmergence()
    {
        float time = Main.GameUpdateCount * 0.035f;
        Vector2 tangent = new(-_surfaceNormal.Y, _surfaceNormal.X);
        Vector2 target = _emergePoint + tangent * (MathF.Sin(time) * 100f)
            + _surfaceNormal * (MathF.Sin(time * 0.7f) * 30f);
        Vector2 desired = (target - NPC.Center).SafeNormalize(Vector2.Zero) * 6f;
        NPC.velocity = Vector2.Lerp(NPC.velocity, desired, 0.07f);
    }

    private void FireWeakHomingVolley(Player player)
    {
        Vector2 baseDirection = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX);

        for (int i = -1; i <= 1; i++)
        {
            Vector2 velocity = baseDirection.RotatedBy(i * 0.13f) * 7f;
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center + baseDirection * 55f,
                velocity,
                ModContent.ProjectileType<FossilShardProjectile>(),
                16,
                0f,
                Main.myPlayer,
                player.whoAmI
            );
        }
    }

    private void FindEmergencePoint(Player player)
    {
        for (int attempt = 0; attempt < 36; attempt++)
        {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 candidate = player.Center + new Vector2(MathF.Cos(angle) * 720f, MathF.Sin(angle) * 390f);
            Point candidateTile = candidate.ToTileCoordinates();

            for (int radius = 0; radius <= 12; radius++)
            {
                for (int x = candidateTile.X - radius; x <= candidateTile.X + radius; x++)
                {
                    for (int y = candidateTile.Y - radius; y <= candidateTile.Y + radius; y++)
                    {
                        if (!WorldGen.InWorld(x, y, 10) || !WorldGen.SolidTile(x, y))
                            continue;

                        Vector2 solidCenter = new Vector2(x * 16f + 8f, y * 16f + 8f);
                        Vector2 normal = (player.Center - solidCenter).SafeNormalize(Vector2.UnitY);

                        for (int step = 1; step <= 10; step++)
                        {
                            Vector2 outside = solidCenter + normal * (step * 16f);
                            Point outsideTile = outside.ToTileCoordinates();
                            if (!WorldGen.InWorld(outsideTile.X, outsideTile.Y, 5))
                                break;

                            if (WorldGen.SolidTile(outsideTile.X, outsideTile.Y))
                                continue;

                            _surfaceNormal = normal;
                            _emergePoint = outside + normal * 50f;
                            _buriedPoint = solidCenter - normal * 110f;
                            return;
                        }
                    }
                }
            }
        }

        Rectangle arena = FossilEncounterSystem.HasArena
            ? FossilEncounterSystem.ArenaBounds
            : new Rectangle((int)player.Center.X - 800, (int)player.Center.Y - 450, 1600, 900);

        bool fromLeft = Main.rand.NextBool();
        _surfaceNormal = fromLeft ? Vector2.UnitX : -Vector2.UnitX;
        _emergePoint = new Vector2(fromLeft ? arena.Left + 90f : arena.Right - 90f, player.Center.Y - 80f);
        _buriedPoint = _emergePoint - _surfaceNormal * 180f;
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
        NPC.velocity.Y += 0.4f;
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

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D skull = ModContent.Request<Texture2D>(SpriteRoot + "LizardSkull", AssetRequestMode.ImmediateLoad).Value;
        Texture2D jawBody = ModContent.Request<Texture2D>(SpriteRoot + "LizardJaw", AssetRequestMode.ImmediateLoad).Value;

        Vector2 drawPos = NPC.Center - screenPos;
        float rotation = NPC.rotation;
        bool facesLeft = MathF.Cos(rotation) < 0f;
        SpriteEffects effects = facesLeft ? SpriteEffects.FlipVertically : SpriteEffects.None;
        
        Vector2 skullOffset = new(0f, -18.5f);
        Vector2 jawOffset = new(-8f, 42f);
        if (facesLeft)
        {
            skullOffset.Y *= -1f;
            jawOffset.Y *= -1f;
        }

        Color color = drawColor * (1f - NPC.alpha / 255f);
        spriteBatch.Draw(skull, drawPos + skullOffset.RotatedBy(rotation), null, color,
            rotation, skull.Size() * 0.5f, 1f, effects, 0f);

        float jawOpen = 0f;
        if ((int)NPC.ai[0] == StateSpit)
        {
            // One bite for each volley: open during the windup and close after firing.
            for (int shotTick = 35; shotTick <= 105; shotTick += 35)
            {
                float pulse = MathHelper.Clamp(1f - MathF.Abs(NPC.ai[1] - shotTick) / 15f, 0f, 1f);
                jawOpen = Math.Max(jawOpen, pulse);
            }
        }

        Vector2 jawPivot = new(28f, 15f);
        if (facesLeft)
            jawPivot.Y = jawBody.Height - jawPivot.Y;

        Vector2 jawCenter = drawPos + jawOffset.RotatedBy(rotation);
        Vector2 jawJoint = jawCenter + (jawPivot - jawBody.Size() * 0.5f).RotatedBy(rotation);
        spriteBatch.Draw(jawBody, jawJoint, null, color,
            rotation + jawOpen * 0.38f * (facesLeft ? -1f : 1f),
            jawPivot, 1f, effects, 0f);

        if (IsHypnosisState)
        {
            Texture2D flare = GetFlareDrawTexture();
            Vector2 eyePosition = FlareWorldPosition - screenPos;
            Vector2 origin = flare.Size() * 0.5f;
            float opacity = 1f - NPC.alpha / 255f;
            spriteBatch.Draw(flare, eyePosition, null, Color.White * opacity,
                0f, origin, FlareScale, SpriteEffects.None, 0f);
        }

        return false;
    }

    private static Texture2D GetFlareDrawTexture()
    {
        if (_flareDrawTexture != null)
            return _flareDrawTexture;

        Texture2D source = ModContent.Request<Texture2D>(SpriteRoot + "LizardEyeFlare", AssetRequestMode.ImmediateLoad).Value;
        Color[] pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        
        bool needsPremultiplication = false;
        foreach (Color pixel in pixels)
        {
            if (pixel.A > 0 && pixel.A < 255 &&
                (pixel.R > pixel.A + 8 || pixel.G > pixel.A + 8 || pixel.B > pixel.A + 8))
            {
                needsPremultiplication = true;
                break;
            }
        }

        if (!needsPremultiplication)
            return _flareDrawTexture = source;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            int alpha = pixel.A;
            pixels[i] = new Color(
                (byte)((pixel.R * alpha + 127) / 255),
                (byte)((pixel.G * alpha + 127) / 255),
                (byte)((pixel.B * alpha + 127) / 255),
                pixel.A);
        }

        _flareDrawTexture = new Texture2D(source.GraphicsDevice, source.Width, source.Height);
        _flareDrawTexture.SetData(pixels);
        _ownsFlareDrawTexture = true;
        return _flareDrawTexture;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        WriteVector(writer, _buriedPoint);
        WriteVector(writer, _emergePoint);
        WriteVector(writer, _surfaceNormal);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        _buriedPoint = ReadVector(reader);
        _emergePoint = ReadVector(reader);
        _surfaceNormal = ReadVector(reader);
    }

    private static void WriteVector(BinaryWriter writer, Vector2 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    private static Vector2 ReadVector(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle());
}
