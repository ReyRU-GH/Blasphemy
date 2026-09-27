using Blasphemy.Projectiles.BloodWeapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Items.Weapons;

public sealed class WoodenStake : BloodWeapon
{
    public override int LifeCost => 15;
    public override int RecoveryPercent => 60;
    public override int PainGain => 10;

    public override void SetDefaults()
    {
        SetWeapon(15, 32, 5f);
        Item.shoot = ModContent.ProjectileType<BloodClotProjectile>();
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        bool special = Agonized(player, Item);
        int count = special ? 5 : 3;
        for (int i = 0; i < count; i++)
            Projectile.NewProjectile(source, player.Center, Aim(player, 5f).RotatedBy((i - (count - 1) / 2f) * 0.18f),
                type, damage, knockback, player.whoAmI, special ? 1f : 0f);
        return false;
    }

    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Wood, 25).AddTile(TileID.WorkBenches).Register();
}
