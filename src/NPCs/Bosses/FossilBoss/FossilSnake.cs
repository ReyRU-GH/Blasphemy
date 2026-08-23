using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.NPCs.Bosses.FossilBoss;

public class FossilSnakeHead : ModNPC
{
    private const int SegmentCount = 16;
    private const int SegmentSpacing = 24;
    private const int StateCircle = 0;
    private const int StateTelegraph = 1;
    private const int StateDash = 2;
    private const int StateSplit = 3;
    private const int StateSparks = 4;
    private const float CircleDistance = 300f;
    private const float CircleSpeed = 8f;
    private const float CircleAttackInterval = 240f;
    private const float TelegraphTime = 45f;
    private const float DashTime = 40f;
    private const float DashSpeed = 16f;
    private const float SplitGroupDelay = 45f;
    private const float LaunchSpeed = 12f;
    private const float SparkTime = 36f;

    private Vector2 _dashTarget;
    private int _sparkRemaining;
    
    public override string Texture => "Blasphemy/src/NPCs/Bosses/FossilBoss/FossilSnakeHead";

    public bool LizardSpawned;
    public bool FishSpawned;

    public override void SetStaticDefaults()
    {
        
    }

    public override void SetDefaults()
    {
        NPC.width = 48;
        NPC.height = 48;
        NPC.damage = 1;
        NPC.defense = 0;
        NPC.lifeMax = 6200;
        NPC.aiStyle = -1;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.knockBackResist = 0f;
        NPC.behindTiles = true;
        NPC.boss = true;
    }

    public override void AI()
    {
        Player player = Main.player[NPC.target];
        if (!player.active || player.dead || Vector2.Distance(NPC.Center, player.Center) > 10000f
            || !player.ZoneUndergroundDesert)
        {
            NPC.TargetClosest(false);
            if (NPC.timeLeft > 30)
                NPC.timeLeft = 30;

            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.velocity.Y += 1f;
            return;
        }

        NPC.timeLeft = NPC.activeTime;

        if (NPC.ai[3] == 0f)
        {
            SpawnSegments(player);
            NPC.ai[3] = 1f;
            NPC.netUpdate = true;
        }

        int state = (int)NPC.ai[0];
        NPC.ai[1] += 1f;

        switch (state)
        {
            case StateCircle:
                Circle(player);
                if (NPC.ai[1] >= CircleAttackInterval)
                {
                    int attack = Main.rand.Next(3);
                    if (attack == 0)
                    {
                        ChangeState(StateTelegraph);
                    }
                    else if (attack == 1)
                    {
                        ChangeState(StateSplit);
                    }
                    else
                    {
                       ChangeState(StateSparks);
                    }
                }
                break;
            case StateTelegraph:
                Telegraph(player);
                break;
            case StateDash:
                Dash();
                break;
            case StateSplit:
                Split(player);
                break;
            case StateSparks:
                Sparks(player);
                break;
            default:
                ChangeState(StateCircle);
                break;
        }

        if (NPC.AnyNPCs(ModContent.NPCType<FossilLizard>()))
        {
            LizardSpawned = true;
        }

        if (!LizardSpawned && NPC.life <= NPC.lifeMax * 0.8f)
        {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<FossilLizard>());
        }
        
        if (NPC.AnyNPCs(ModContent.NPCType<FossilFish>()))
        {
            FishSpawned = true;
        }
        if (!FishSpawned && NPC.life <= NPC.lifeMax * 0.5f)
        {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<FossilFish>());
        }
        
        NPC.rotation = NPC.velocity.ToRotation();
    }

    private void SpawnSegments(Player player)
    {
        int previous = NPC.whoAmI;
        int bodyType = ModContent.NPCType<FossilSnakeBody>();
        int tailType = ModContent.NPCType<FossilSnakeTail>();

        for (int i = 1; i < SegmentCount; i++)
        {
            int type = i == SegmentCount - 1 ? tailType : bodyType;
            int index = NPC.NewNPC(
                NPC.GetSource_FromAI(),
                (int)NPC.Center.X - i * SegmentSpacing,
                (int)NPC.Center.Y,
                type,
                0,
                previous + 1,
                i,
                NPC.whoAmI + 1,
                0f,
                player.whoAmI);

            if (index < 0 || index >= Main.maxNPCs)
            {
                break;
            }

            Main.npc[index].netUpdate = true;
            previous = index;
        }
    }

    private void ChangeState(int state)
    {
        NPC.ai[0] = state;
        NPC.ai[1] = 0f;
        NPC.localAI[1] = -1f;
        NPC.netUpdate = true;
    }

    private void Circle(Player player)
    {
        NPC.localAI[0] += 0.025f;
        float angle = NPC.localAI[0];
        Vector2 orbit = player.Center + new Vector2((float)Math.Cos(angle) * CircleDistance, (float)Math.Sin(angle) * CircleDistance * 0.6f);
        MoveToward(orbit, CircleSpeed, 0.08f);
    }

    private void Telegraph(Player player)
    {
        if (NPC.ai[1] <= 1f)
        {
            _dashTarget = player.Center;
        }

        NPC.velocity *= 0.85f;

        if (NPC.ai[1] >= TelegraphTime)
        {
            ChangeState(StateDash);
        }
    }

    private void Dash()
    {
        MoveToward(_dashTarget, DashSpeed, 0.3f);

        if (NPC.ai[1] >= DashTime || Vector2.Distance(NPC.Center, _dashTarget) < 32f)
        {
            ChangeState(StateCircle);
        }
    }

    private void Split(Player player)
    {
        int currentGroup = (int)(NPC.ai[1] / SplitGroupDelay);

        if (currentGroup >= 4)
        {
            SetSegmentModes(-1);
            ChangeState(StateCircle);
            return;
        }

        int lastGroup = (int)NPC.localAI[1];
        if (currentGroup != lastGroup)
        {
            // SetSegmentModes(currentGroup);
            NPC.localAI[1] = currentGroup;
            NPC.netUpdate = true;
        }

        if (currentGroup == 0)
        {
            MoveToward(player.Center, LaunchSpeed * 0.5f, 0.12f);
        }
        else
        {
            NPC.velocity *= 0.92f;
        }
    }

    private void Sparks(Player player)
    {
        if (NPC.ai[1] <= 1f)
        {
            _sparkRemaining = Main.rand.Next(2, 4);
        }
    
        NPC.velocity *= 0.9f;
    
        if ((int)NPC.ai[1] % 12 == 0 && _sparkRemaining > 0)
        {
           SpawnSpark(player);
            _sparkRemaining--;
        }
    
        if (_sparkRemaining <= 0 && NPC.ai[1] >= SparkTime)
        {
            ChangeState(StateCircle);
        }
    }

    private void SpawnSpark(Player player)
    {
        Vector2 direction = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
        direction = direction.RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f));
        Vector2 velocity = direction * Main.rand.NextFloat(3f, 5f);
        Dust.NewDust(NPC.Center, 8, 8, DustID.Torch, velocity.X, velocity.Y, 100, default, 1.2f);
    }

    private void SetSegmentModes(int activeGroup)
    {
        int headId = NPC.whoAmI + 1;
        int bodyType = ModContent.NPCType<FossilSnakeBody>();
        int tailType = ModContent.NPCType<FossilSnakeTail>();

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC segment = Main.npc[i];
            if (!segment.active || segment.whoAmI == NPC.whoAmI)
            {
                continue;
            }

            if (segment.type != bodyType && segment.type != tailType)
            {
                continue;
            }

            if ((int)segment.ai[2] != headId)
            {
                continue;
            }

            int segmentIndex = (int)segment.ai[1];
            int group = segmentIndex / 4;
            segment.ai[3] = group == activeGroup ? 1f : 0f;
            segment.netUpdate = true;
        }
    }

    private void MoveToward(Vector2 target, float speed, float acceleration)
    {
        Vector2 direction = target - NPC.Center;
        NPC.velocity = Vector2.Lerp(NPC.velocity, direction.SafeNormalize(Vector2.Zero) * speed, acceleration);
    }
}

public class FossilSnakeBody : ModNPC
{
    private const int SegmentSpacing = 20;
    private const float FollowSpeed = 10f;
    private const float LaunchSpeed = 12f;

    public override string Texture => "Blasphemy/src/NPCs/Bosses/FossilBoss/FossilSnakeBody";

    public override void SetStaticDefaults()
    {
    }

    public override void SetDefaults()
    {
        NPC.width = 32;
        NPC.height = 32;
        NPC.damage = 1;
        NPC.defense = 0;
        NPC.lifeMax = 1;
        NPC.aiStyle = -1;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.knockBackResist = 0f;
        NPC.behindTiles = true;
        NPC.dontTakeDamage = true;
    }

    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;

        int previousIndex = (int)NPC.ai[0] - 1;
        int headIndex = (int)NPC.ai[2] - 1;

        if (!IsValid(previousIndex) || !IsValid(headIndex) || Main.npc[headIndex].type != ModContent.NPCType<FossilSnakeHead>())
        {
            NPC.active = false;
            return;
        }

        NPC head = Main.npc[headIndex];

        if (NPC.ai[3] == 1f)
        {
            Launch(head);
        }
        else
        {
            Follow(Main.npc[previousIndex]);
        }

        if (Math.Abs(NPC.velocity.X) > 0.1f)
        {
            NPC.spriteDirection = NPC.velocity.X > 0f ? 1 : -1;
        }
    }

    private void Follow(NPC previous)
    {
        Vector2 direction = previous.Center - NPC.Center;
        float distance = direction.Length();

        if (distance > SegmentSpacing)
        {
            float speed = MathHelper.Min(distance * 0.25f, FollowSpeed);
            NPC.velocity = Vector2.Lerp(NPC.velocity, direction.SafeNormalize(Vector2.Zero) * speed, 0.35f);
        }
        else
        {
            NPC.velocity *= 0.8f;
        }

        NPC.rotation = direction.ToRotation();
    }

    private void Launch(NPC head)
    {
        if (head.target >= 0 && head.target < Main.maxPlayers && Main.player[head.target].active && !Main.player[head.target].dead)
        {
            Player player = Main.player[head.target];
            Vector2 direction = player.Center - NPC.Center;
            NPC.velocity = Vector2.Lerp(NPC.velocity, direction.SafeNormalize(Vector2.Zero) * LaunchSpeed, 0.15f);
            NPC.rotation = direction.ToRotation();
        }
        else
        {
            Follow(Main.npc[(int)NPC.ai[0] - 1]);
        }
    }

    private bool IsValid(int index) => index >= 0 && index < Main.maxNPCs && Main.npc[index].active;
}

public class FossilSnakeTail : ModNPC
{
    private const int SegmentSpacing = 24;
    private const float FollowSpeed = 10f;
    private const float LaunchSpeed = 12f;

    public override string Texture => "Blasphemy/src/NPCs/Bosses/FossilBoss/FossilSnakeTail";

    public override void SetStaticDefaults()
    {
    }

    public override void SetDefaults()
    {
        NPC.width = 36;
        NPC.height = 36;
        NPC.damage = 1;
        NPC.defense = 0;
        NPC.lifeMax = 1;
        NPC.aiStyle = -1;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.knockBackResist = 0f;
        NPC.behindTiles = true;
        NPC.dontTakeDamage = true;
    }

    public override void AI()
    {
        NPC.timeLeft = NPC.activeTime;

        int previousIndex = (int)NPC.ai[0] - 1;
        int headIndex = (int)NPC.ai[2] - 1;

        if (!IsValid(previousIndex) || !IsValid(headIndex) || Main.npc[headIndex].type != ModContent.NPCType<FossilSnakeHead>())
        {
            NPC.active = false;
            return;
        }

        NPC head = Main.npc[headIndex];

        if (NPC.ai[3] == 1f)
        {
            Launch(head);
        }
        else
        {
            Follow(Main.npc[previousIndex]);
        }

        if (Math.Abs(NPC.velocity.X) > 0.1f)
        {
            NPC.spriteDirection = NPC.velocity.X > 0f ? 1 : -1;
        }
    }

    private void Follow(NPC previous)
    {
        Vector2 direction = previous.Center - NPC.Center;
        float distance = direction.Length();

        if (distance > SegmentSpacing)
        {
            float speed = MathHelper.Min(distance * 0.25f, FollowSpeed);
            NPC.velocity = Vector2.Lerp(NPC.velocity, direction.SafeNormalize(Vector2.Zero) * speed, 0.35f);
        }
        else
        {
            NPC.velocity *= 0.8f;
        }

        NPC.rotation = direction.ToRotation();
    }

    private void Launch(NPC head)
    {
        if (head.target >= 0 && head.target < Main.maxPlayers && Main.player[head.target].active && !Main.player[head.target].dead)
        {
            Player player = Main.player[head.target];
            Vector2 direction = player.Center - NPC.Center;
            NPC.velocity = Vector2.Lerp(NPC.velocity, direction.SafeNormalize(Vector2.Zero) * LaunchSpeed, 0.15f);
            NPC.rotation = direction.ToRotation();
        }
        else
        {
            Follow(Main.npc[(int)NPC.ai[0] - 1]);
        }
    }

    private bool IsValid(int index) => index >= 0 && index < Main.maxNPCs && Main.npc[index].active;
}