using Microsoft.Xna.Framework;
using Terraria;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class GroundGlassProjectile : BloodWeaponProjectile
{
    public override string Texture => "Blasphemy/Assets/Textures/Projectiles/BloodWeapons/GroundGlassProjectile";
    protected override Color Tint => new(180, 220, 235, 210);
    private bool _landed;
    public override void SetDefaults()
    {
        Standard(10, 8, 900);
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
    }
    public override void AI()
    {
        if (!_landed) Projectile.velocity.Y += 0.28f;
        else Projectile.velocity = Vector2.Zero;
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        _landed = true;
        Projectile.tileCollide = false;
        Projectile.velocity = Vector2.Zero;
        Projectile.netUpdate = true;
        return false;
    }
    public override bool CanHitPlayer(Player target) => true;
    public override bool PreDraw(ref Color lightColor)
    {
        DrawSprite(0.24f, Projectile.rotation, Color.White);
        return false;
    }
}
