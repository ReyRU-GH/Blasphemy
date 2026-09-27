using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Projectiles;

/// <summary>
/// Small, weak homing fossil shard used by the lizard.
/// </summary>
public sealed class FossilShardProjectile : ModProjectile
{
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Bone}";

    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 14;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 240;
        Projectile.damage = 16;
    }

    public override void AI()
    {
        int targetIndex = (int)Projectile.ai[0];
        if (targetIndex >= 0 && targetIndex < Main.maxPlayers)
        {
            Player target = Main.player[targetIndex];
            if (target.active && !target.dead && Projectile.timeLeft > 90)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 7.5f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.025f);
            }
        }

        Projectile.rotation += 0.16f * (Projectile.velocity.X >= 0f ? 1f : -1f);

        if (Main.rand.NextBool(4))
            Dust.NewDustPerfect(Projectile.Center, DustID.Stone, -Projectile.velocity * 0.05f, 100, default, 0.9f);
    }
}
