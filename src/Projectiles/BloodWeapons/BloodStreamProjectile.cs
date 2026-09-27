using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class BloodStreamProjectile : BloodWeaponProjectile
{
    public override string Texture => "Blasphemy/Assets/Textures/Projectiles/BloodWeapons/BloodStreamProjectile";
    protected override Color Tint => new(155, 5, 20, 220);
    public override void SetDefaults() { Standard(8, 8, 90); Projectile.tileCollide = true; }
    public override bool PreDraw(ref Color lightColor)
    {
        DrawSprite(0.18f, Projectile.velocity.ToRotation() + MathHelper.PiOver2, Color.White);
        return false;
    }
    public override void AI()
    {
        Projectile.velocity.Y += 0.22f;
        if (Main.rand.NextBool(3))
            Dust.NewDustPerfect(Projectile.Center, DustID.Blood, -Projectile.velocity * 0.1f);
    }
}
