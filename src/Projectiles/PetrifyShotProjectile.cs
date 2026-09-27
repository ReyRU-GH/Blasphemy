using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Projectiles;

/// <summary>
/// Slow fossil projectile fired by the snake. On hit it applies the vanilla Stoned debuff for a short duration.
/// </summary>
public sealed class PetrifyShotProjectile : ModProjectile
{
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Boulder}";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
    }

    public override void SetDefaults()
    {
        Projectile.width = 24;
        Projectile.height = 24;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 300;
        Projectile.tileCollide = true;
        Projectile.extraUpdates = 1;
        Projectile.damage = 14;
        Projectile.knockBack = 1f;
    }

    public override void AI()
    {
        if (Main.rand.NextBool(3))
        {
            Dust.NewDustPerfect(
                Projectile.Center,
                DustID.Stone,
                Projectile.velocity.RotatedByRandom(0.45f) * 0.25f,
                80,
                default,
                1.15f
            );
        }

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        int duration = Main.masterMode ? 150 : Main.expertMode ? 120 : 90;
        target.AddBuff(BuffID.Stoned, duration);
    }

    public override void OnKill(int timeLeft)
    {
        Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
        SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

        for (int i = 0; i < 8; i++)
        {
            Dust.NewDustPerfect(
                Projectile.Center,
                DustID.Stone,
                Main.rand.NextVector2Circular(3f, 3f),
                80,
                default,
                1.35f
            );
        }
    }
}
