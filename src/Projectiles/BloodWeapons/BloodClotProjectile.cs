using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class BloodClotProjectile : BloodWeaponProjectile
{
    public override string Texture => "Blasphemy/Assets/Textures/Projectiles/BloodWeapons/BloodClotProjectile";
    protected override Color Tint => new(170, 20, 35, 190);
    public override void SetDefaults() { Standard(12, 12, 480); Projectile.tileCollide = true; }
    public override bool PreDraw(ref Color lightColor)
    {
        DrawSprite(0.38f, Projectile.velocity.ToRotation(), Color.White);
        return false;
    }
    public override void AI()
    {
        Projectile.ai[1]++;
        NPC target = BloodProjectileUtil.Nearest(Projectile.Center, 350f);
        if (target != null)
            Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 5f, 0.018f);
        Projectile.velocity *= 0.995f;
        if (Projectile.ai[0] == 1f && Projectile.ai[1] == 30f && Projectile.owner == Main.myPlayer)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 direction = (Main.MouseWorld - Projectile.Center).SafeNormalize(new Vector2(owner.direction, 0f));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, direction,
                ModContent.ProjectileType<BloodRayProjectile>(), Projectile.damage, 0f, Projectile.owner);
        }
        if (Projectile.ai[0] == 1f && Projectile.ai[1] >= 30f)
            Projectile.Kill();
    }
}
