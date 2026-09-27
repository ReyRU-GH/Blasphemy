using Blasphemy.DamageClass;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Blasphemy.Projectiles.BloodWeapons;

public abstract class BloodWeaponProjectile : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    protected abstract Color Tint { get; }

    protected void DrawSprite(float scale, float rotation, Color color)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, color,
            rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        BloodProjectileUtil.DrawPixel(Main.spriteBatch, Projectile.Center - Main.screenPosition,
            new Vector2(Projectile.width, Projectile.height), Tint);
        return false;
    }

    protected void Standard(int width, int height, int lifetime)
    {
        Projectile.width = width;
        Projectile.height = height;
        Projectile.friendly = true;
        Projectile.DamageType = ModContent.GetInstance<SacrificialDamage>();
        Projectile.penetrate = 1;
        Projectile.timeLeft = lifetime;
        Projectile.ignoreWater = true;
    }
}
