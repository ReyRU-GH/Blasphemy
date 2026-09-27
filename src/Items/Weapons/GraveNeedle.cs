using Blasphemy.Projectiles.BloodWeapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Blasphemy.Items.Weapons;

public sealed class GraveNeedle : BloodWeapon
{
    public override int LifeCost => 10;
    public override int RecoveryPercent => 0;
    public override int PainGain => 13;
    public override void SetDefaults()
    {
        SetWeapon(15, 32, 5f);
        Item.shoot = ModContent.ProjectileType<BloodCloudProjectile>();
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        bool special = Agonized(player, Item);
        Projectile.NewProjectile(source, player.Center, special ? Vector2.Zero : Aim(player, 5f),
            type, damage, knockback, player.whoAmI, special ? 1f : 0f);
        return false;
    }
}
