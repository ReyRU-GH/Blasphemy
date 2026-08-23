using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.NPCs.Bosses.FossilBoss;

public class FossilFish : ModNPC
{
    private const float DepthOffset = 70f;
    private const float MoveSpeed = 5f;

    public override string Texture => "Blasphemy/src/NPCs/Bosses/FossilBoss/FossilFish";
    
    private const int AiState = 0;
    private const int AiTimer = 1;
    private const int AiSurfaceX = 2;
    private const int AiSurfaceY = 3;
        
    private const int StateUnderSurface = 0;
    private const int StateEmerging = 1;
    private const int StateAttacking = 2;
    private const int StateRetreating = 3;
        
    private const float PatrolRadius = 300f;
    private const float AttackCooldown = 180f;
    private const float EmergeSpeed = 8f;
    private const float PatrolSpeed = 6f;
    private const float RetreatSpeed = 10f;

    public bool IsLeftAlone;
    public int RandomDirection = Main.rand.NextBool() ? 1 : -1;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[NPC.type] = 1;
        NPCID.Sets.NoMultiplayerSmoothingByType[NPC.type] = true;
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.MustAlwaysDraw[Type] = true;

        NPCID.Sets.BossBestiaryPriority.Add(NPC.type);
        
    }

    public override void SetDefaults()
    {
        NPC.width = 200;
        NPC.height = 100;
        NPC.damage = 1;
        NPC.defense = 0;
        NPC.lifeMax = 3100;
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
        NPC.ai[AiTimer] += 1f;

        int state = (int)NPC.ai[AiState];

        switch (state)
        {
            case StateUnderSurface:
                HandleUnderSurface(player);
                break;
                    
            case StateEmerging:
                HandleEmerging(player);
                break;
                    
            case StateAttacking:
                HandleAttacking(player);
                break;
            case StateRetreating:
                HandleRetreating(player);
                break;
        }

        if (!NPC.AnyNPCs(ModContent.NPCType<FossilSnakeHead>()) && !NPC.AnyNPCs(ModContent.NPCType<FossilLizard>()))
        {
            IsLeftAlone = true;
        }

        
        NPC.rotation = NPC.velocity.X > 0f ? NPC.velocity.ToRotation() : NPC.velocity.ToRotation() + MathHelper.Pi;
        NPC.spriteDirection = NPC.velocity.X > 0f ? -1 :  1;
    }
    private void HandleUnderSurface(Player player)
    {
        if (NPC.ai[AiTimer] <= 1f)
        {
            if (!HasValidSurface())
            {
                FindSurfaceUnderPlayer(player);
            }
        }

        if (!HasValidSurface())
        {
            FindSurfaceUnderPlayer(player);
            if (!HasValidSurface())
            {
                return;
            }
        }

        Vector2 surfacePos = GetSurfacePosition();
        Vector2 patrolCenter = new Vector2(player.Center.X, surfacePos.Y + 5f);
        
        float distanceToPlayer = Vector2.Distance(NPC.Center, player.Center);
        float playerAboveSurface = surfacePos.Y - player.Center.Y;

        if (distanceToPlayer < 200f && playerAboveSurface > 40f && playerAboveSurface < 150f)
        {
            if (NPC.ai[AiTimer] >= AttackCooldown)
            {
                NPC.ai[AiState] = StateEmerging;
                NPC.ai[AiTimer] = 0f;
                NPC.netUpdate = true;
                return;
            }
        }

        NPC.localAI[0] += 0.02f;
        float angle = NPC.localAI[0];
        Vector2 patrolTarget = patrolCenter + new Vector2(
            (float)Math.Cos(angle) * PatrolRadius,
            (float)Math.Sin(angle * 2f) * 40f
        );

        MoveToward(patrolTarget, PatrolSpeed, 0.08f);
    }

    private void HandleEmerging(Player player)
    {
        Vector2 surfacePos = GetSurfacePosition();
        Vector2 emergeTarget = new Vector2();
        float surfacePosModfiier = IsLeftAlone ? 30 : 30;
        float distanceModifier = IsLeftAlone ? 20 : 50;
        if (IsLeftAlone)
        {
            emergeTarget = new Vector2(player.Center.X + 600 * RandomDirection, surfacePos.Y - surfacePosModfiier);
            Dust.NewDust(emergeTarget, 30, 30, DustID.GreenFairy, 0, 0);
        }
        else
        {
            emergeTarget = new Vector2(player.Center.X, surfacePos.Y - surfacePosModfiier);
        }

        MoveToward(emergeTarget, EmergeSpeed * 3f, 0.15f);

        if (NPC.Center.Y < surfacePos.Y - surfacePosModfiier || Vector2.Distance(NPC.Center, emergeTarget) < distanceModifier)
        {
            if (IsLeftAlone)
            {
                NPC.ai[AiState] = StateAttacking;
            }
            else
            {
                NPC.ai[AiState] = StateRetreating;
            }
            
            NPC.ai[AiTimer] = 0f;
            RandomDirection = Main.rand.NextBool() ? 1 : -1;
            NPC.netUpdate = true;
        }
    }

    
    private void HandleAttacking(Player player)
    {
        Vector2 dashTarget = player.Center;
        float SpeedModifier = IsLeftAlone ? 20f : 1.5f;
        MoveToward(dashTarget, EmergeSpeed * SpeedModifier, 0.2f);

        if (NPC.ai[AiTimer] >= 10f)
        {
            NPC.ai[AiState] = StateRetreating;
            NPC.ai[AiTimer] = 0f;
            NPC.netUpdate = true;
        }
    }

    private void HandleRetreating(Player player)
    {
        if (!HasValidSurface())
        {
            FindSurfaceUnderPlayer(player);
            if (!HasValidSurface())
            {
                NPC.ai[AiState] = StateUnderSurface;
                NPC.ai[AiTimer] = 0f;
                NPC.netUpdate = true;
                return;
            }
        }

        Vector2 surfacePos = GetSurfacePosition();
        Vector2 retreatTarget = new Vector2(NPC.Center.X, surfacePos.Y + 90f);

        MoveToward(retreatTarget, RetreatSpeed, 0.12f);

        if (NPC.Center.Y > surfacePos.Y + 100f)
        {
            NPC.ai[AiState] = StateUnderSurface;
            NPC.ai[AiTimer] = 0f;
            NPC.netUpdate = true;
        }
    }

    private void FindSurfaceUnderPlayer(Player player)
    {
        Point playerTile = player.Center.ToTileCoordinates();
        
        int searchStartX = playerTile.X - 15;
        int searchEndX = playerTile.X + 15;
        int searchStartY = playerTile.Y;
        int searchEndY = playerTile.Y + 100;

        for (int y = searchStartY; y < searchEndY; y++)
        {
            for (int x = searchStartX; x < searchEndX; x++)
            {
                if (IsValidSurface(x, y))
                {
                    StoreSurfacePosition(x, y);
                    return;
                }
            }
        }

        for (int y = searchStartY; y > searchStartY - 50; y--)
        {
            for (int x = searchStartX; x < searchEndX; x++)
            {
                if (IsValidSurface(x, y))
                {
                    StoreSurfacePosition(x, y);
                    return;
                }
            }
        }
    }

    private bool IsValidSurface(int x, int y)
    {
        if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
        {
            return false;
        }

        Tile tile = Main.tile[x, y];
        
        if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType])
        {
            return false;
        }

        if (y > 0)
        {
            Tile aboveTile = Main.tile[x, y - 1];
            if (aboveTile.HasUnactuatedTile && Main.tileSolid[aboveTile.TileType])
            {
                return false;
            }
        }

        int flatCount = 0;
        for (int dx = -2; dx <= 2; dx++)
        {
            int checkX = x + dx;
            if (checkX >= 0 && checkX < Main.maxTilesX)
            {
                Tile checkTile = Main.tile[checkX, y];
                if (checkTile.HasUnactuatedTile && Main.tileSolid[checkTile.TileType])
                {
                    flatCount++;
                }
            }
        }

        return flatCount >= 3;
    }

    private void StoreSurfacePosition(int x, int y)
    {
        NPC.ai[AiSurfaceX] = x;
        NPC.ai[AiSurfaceY] = y;
        NPC.netUpdate = true;
    }

    private bool HasValidSurface()
    {
        int x = (int)NPC.ai[AiSurfaceX];
        int y = (int)NPC.ai[AiSurfaceY];
        
        return x > 0 && y > 0 && x < Main.maxTilesX && y < Main.maxTilesY;
    }

    private Vector2 GetSurfacePosition()
    {
        int x = (int)NPC.ai[AiSurfaceX];
        int y = (int)NPC.ai[AiSurfaceY];
        return new Vector2(x * 16 + 8, y * 16 + 8);
    }

    private void MoveToward(Vector2 target, float speed, float acceleration)
    {
        Vector2 direction = target - NPC.Center;
        NPC.velocity = Vector2.Lerp(
            NPC.velocity,
            direction.SafeNormalize(Vector2.Zero) * speed,
            acceleration
        );
    }
}