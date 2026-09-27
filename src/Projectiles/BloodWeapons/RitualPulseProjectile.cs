using Microsoft.Xna.Framework;
using Terraria;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class RitualPulseProjectile : BloodWeaponProjectile
{
    public override string Texture => "Blasphemy/Assets/Textures/Projectiles/BloodWeapons/RitualPulseProjectile";
    protected override Color Tint => new(165, 0, 35, 180);
    public override void SetDefaults() { Standard(48, 48, 2); Projectile.tileCollide = false; }
    public override bool? CanDamage() => true;
    public override bool? CanHitNPC(NPC target) => target.whoAmI + 1 == (int)Projectile.ai[0];
    public override bool PreDraw(ref Color lightColor)
    {
        DrawSprite(0.7f, 0f, Color.White);
        return false;
    }
}
