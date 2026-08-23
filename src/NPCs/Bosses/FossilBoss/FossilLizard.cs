    using Microsoft.Xna.Framework;
    using Terraria;
    using Terraria.ID;
    using Terraria.ModLoader;

    namespace Blasphemy.NPCs.Bosses.FossilBoss;

    public class FossilLizard : ModNPC
    {
        private const float HoverDistanceX = 260f;
        private const float HoverDistanceY = 220f;
        private const float MoveSpeed = 6f;

        public override string Texture => "Blasphemy/src/NPCs/Bosses/FossilBoss/FossilLizard";

        public bool FishSpawned;

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
            NPC.width = 64;
            NPC.height = 48;
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
            NPC.ai[1] += 1f;

            if (NPC.ai[1] >= 600f)
            {
                NPC.ai[0] = NPC.ai[0] == 0f ? 1f : 0f;
                NPC.ai[1] = 0f;
                NPC.netUpdate = true;
            }
            if (NPC.AnyNPCs(ModContent.NPCType<FossilFish>()))
            {
                FishSpawned = true;
            }
            
            if (!FishSpawned && NPC.life <= NPC.lifeMax * 0.5f)
            {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<FossilFish>());
            }

            float side = NPC.ai[0] == 0f ? -1f : 1f;
            Vector2 hover = player.Center + new Vector2(side * HoverDistanceX, -HoverDistanceY);
            Vector2 direction = hover - NPC.Center;
            NPC.velocity = Vector2.Lerp(NPC.velocity, direction.SafeNormalize(Vector2.Zero) * MoveSpeed, 0.06f);
            NPC.spriteDirection = player.Center.X > NPC.Center.X ? 1 : -1;
        }
        
    }