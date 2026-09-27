using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class AncientBoneProjectile : BloodWeaponProjectile
{
    public override string Texture => "Blasphemy/Assets/Textures/Projectiles/BloodWeapons/AncientBoneProjectile";
    protected override Color Tint => new(205, 190, 160, 230);
    public override void SetDefaults() { Standard(12, 12, 300); Projectile.tileCollide = true; Projectile.penetrate = -1; }
    public override bool PreDraw(ref Color lightColor)
    {
        DrawSprite(0.4f, Projectile.rotation, Color.White);
        return false;
    }
    public override void AI()
    {
        if (Projectile.ai[1] > 0f)
        {
            int index = (int)Projectile.ai[1] - 1;
            if (index < 0 || index >= Main.maxNPCs || !Main.npc[index].active) { Projectile.Kill(); return; }
            NPC target = Main.npc[index];
            Projectile.Center = target.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.ai[2]++;
            if ((int)Projectile.ai[2] % 60 == 0 && Projectile.owner == Main.myPlayer)
                Main.player[Projectile.owner].ApplyDamageToNPC(target, Math.Max(1, Projectile.damage / 3), 0f, 0,
                    damageType: Projectile.DamageType);
            if (Projectile.ai[2] >= 420f) Projectile.Kill();
            return;
        }
        Projectile.velocity.Y += 0.10f;
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.ai[0] != 1f) { Projectile.Kill(); return; }
        Projectile.ai[1] = target.whoAmI + 1;
        Projectile.ai[2] = 0f;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 425;
        Projectile.netUpdate = true;
    }
    public override bool? CanDamage() => Projectile.ai[1] > 0f ? false : null;
}
