using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class VoidTentacleProjectile : BloodWeaponProjectile
{
    public override string Texture => "Blasphemy/Assets/Textures/Projectiles/BloodWeapons/VoidTentacleProjectile";
    protected override Color Tint => new(45, 15, 70, 220);
    private Vector2 Origin => new(Projectile.ai[0], Projectile.ai[1]);
    private Vector2 BendPoint
    {
        get
        {
            Vector2 middle = (Origin + Projectile.Center) * 0.5f;
            NPC target = BloodProjectileUtil.Nearest(middle, 260f);
            return target == null ? middle : middle +
                (target.Center - middle).SafeNormalize(Vector2.Zero) * 25f;
        }
    }
    public override void SetDefaults()
    {
        Standard(12, 12, 45);
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 25;
    }
    public override void AI()
    {
        NPC target = BloodProjectileUtil.Nearest(Projectile.Center, 260f);
        if (target != null)
            Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * Projectile.velocity.Length(), 0.035f);
        float maxDistance = Projectile.ai[2] == 1f ? 288f : 192f;
        if (Vector2.DistanceSquared(Origin, Projectile.Center) >= maxDistance * maxDistance) Projectile.Kill();
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        BloodProjectileUtil.HitsLine(targetHitbox, Origin, BendPoint, 13f) ||
        BloodProjectileUtil.HitsLine(targetHitbox, BendPoint, Projectile.Center, 13f);
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Vector2 reach = Projectile.Center - Origin;
        if (reach.LengthSquared() > 1f)
            Main.spriteBatch.Draw(texture, Origin - Main.screenPosition, null, Color.White,
                reach.ToRotation() + MathHelper.PiOver2,
                new Vector2(texture.Width * 0.5f, texture.Height),
                new Vector2(0.36f, reach.Length() / texture.Height), SpriteEffects.None, 0f);
        return false;
    }
}
