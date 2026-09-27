using Blasphemy.Projectiles.BloodWeapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Blasphemy.Items.Weapons;

public sealed class VoodooDoll : BloodWeapon
{
    public override int LifeCost => 5;
    public override int RecoveryPercent => 40;
    public override int PainGain => 3;
    public override void SetDefaults()
    {
        SetWeapon(25, 15, 11f);
        Item.shoot = ModContent.ProjectileType<VoodooSkullProjectile>();
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        bool low = player.statLife < player.statLifeMax2 / 2;
        int count = Agonized(player, Item) ? Main.rand.Next(low ? 8 : 6, low ? 13 : 9) : low ? 3 : 1;
        float aim = Aim(player, 1f).ToRotation();
        for (int i = 0; i < count; i++)
        {
            float spread = count == 1 ? 0f : MathHelper.Lerp(-MathHelper.Pi / 6f, MathHelper.Pi / 6f, i / (float)(count - 1));
            Vector2 shot = (aim + spread + Main.rand.NextFloat(-0.05f, 0.05f)).ToRotationVector2() * 11f;
            Projectile.NewProjectile(source, player.Center, shot, type, damage, knockback, player.whoAmI);
        }
        return false;
    }
}
