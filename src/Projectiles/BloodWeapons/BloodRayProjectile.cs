using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class BloodRayProjectile : BloodWeaponProjectile
{
    private const float MaxLength = 100f * 16f;
    private const float BeamWidth = 10f;
    public override string Texture => "Blasphemy/Assets/Textures/Projectiles/BloodWeapons/BloodStreamProjectile";
    protected override Color Tint => Color.DarkRed;
    private float _length;
    public override void SetDefaults()
    {
        Standard(10, 10, 24);
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        float[] samples = new float[3];
        Collision.LaserScan(Projectile.Center, direction, BeamWidth, MaxLength, samples);
        _length = MathHelper.Clamp(Math.Min(samples[0], Math.Min(samples[1], samples[2])), 0f, MaxLength);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        _length > 0f && BloodProjectileUtil.HitsLine(targetHitbox, Projectile.Center,
            Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * _length, BeamWidth);
    public override bool PreDraw(ref Color lightColor)
    {
        if (_length <= 0f) return false;
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        float opacity = MathHelper.Clamp(Projectile.timeLeft / 6f, 0f, 1f);
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.White * opacity,
            Projectile.velocity.ToRotation() - MathHelper.PiOver2,
            new Vector2(texture.Width * 0.5f, 0f),
            new Vector2(BeamWidth / texture.Width, _length / texture.Height), SpriteEffects.None, 0f);
        return false;
    }
}
