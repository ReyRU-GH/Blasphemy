using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class ThrownGlassProjectile : BloodWeaponProjectile
{
    public override string Texture => "Blasphemy/Assets/Textures/Projectiles/BloodWeapons/ThrownGlassProjectile";
    protected override Color Tint => new(190, 230, 245, 220);
    public override void SetDefaults() { Standard(12, 12, 240); Projectile.tileCollide = true; }
    public override bool PreDraw(ref Color lightColor)
    {
        DrawSprite(0.38f, Projectile.rotation, Color.White);
        return false;
    }
    public override void AI() { Projectile.velocity.Y += 0.32f; Projectile.rotation += Projectile.velocity.X * 0.04f; }
    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner != Main.myPlayer) return;
        int count = Main.rand.Next(3, 6);
        for (int i = 0; i < count; i++)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-4f, -1f)),
                ModContent.ProjectileType<GroundGlassProjectile>(), Math.Max(1, Projectile.damage / 3),
                0f, Projectile.owner);
    }
}
