using Microsoft.Xna.Framework;
using Terraria;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class VoodooSkullProjectile : BloodWeaponProjectile
{
    public override string Texture => "Blasphemy/Assets/Textures/Projectiles/BloodWeapons/VoodooSkullProjectile";
    protected override Color Tint => new(210, 195, 165, 220);
    public override void SetDefaults() { Standard(18, 18, 240); Projectile.tileCollide = false; }
    public override bool PreDraw(ref Color lightColor)
    {
        DrawSprite(0.5f, Projectile.rotation, Color.White);
        return false;
    }
    public override void AI()
    {
        NPC target = BloodProjectileUtil.Nearest(Projectile.Center, 420f);
        if (target != null)
            Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 11f, 0.025f);
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
}
