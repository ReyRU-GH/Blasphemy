using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class BloodStreamController : BloodWeaponProjectile
{
    protected override Color Tint => Color.Transparent;
    public override void SetDefaults()
    {
        Standard(2, 2, 90);
        Projectile.friendly = false;
        Projectile.tileCollide = false;
    }
    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        Projectile.Center = owner.Center;
        owner.itemAnimation = 2;
        owner.itemTime = 2;
        owner.heldProj = Projectile.whoAmI;
        Projectile.ai[0]++;
        if (Projectile.owner == Main.myPlayer && (int)Projectile.ai[0] % 2 == 0)
        {
            float pressure = MathHelper.Lerp(1f, 0.25f, Projectile.ai[0] / 90f);
            Vector2 aim = (Main.MouseWorld - owner.Center).SafeNormalize(new Vector2(owner.direction, 0f));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.Center,
                aim.RotatedBy(Main.rand.NextFloat(-0.07f, 0.07f)) * (16f * pressure),
                ModContent.ProjectileType<BloodStreamProjectile>(), Projectile.damage * 2,
                Projectile.knockBack, Projectile.owner);
        }
    }
}
