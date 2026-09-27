using System;
using Blasphemy.Players;
using Blasphemy.Projectiles.BloodWeapons;
using Blasphemy.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Items.Weapons;

public abstract class MetalBlade : BloodWeapon, BlasphemySystem.IContextualLifeCostItem
{
    protected abstract int NormalCost { get; }
    protected abstract int AgonyCost { get; }
    protected abstract int BarType { get; }
    public override int LifeCost => NormalCost;
    public override int RecoveryPercent => 70;
    public override int PainGain => 3;
    public int GetLifeCost(Player player) => player.GetModPlayer<BlasphemyPlayer>().IsAgonized ? AgonyCost : NormalCost;
    public int GetRecoveryPercent(Player player) => 70;

    public override void SetDefaults()
    {
        SetWeapon(12, 8, 13f);
        Item.shoot = ModContent.ProjectileType<BloodStreamProjectile>();
    }

    public override bool CanUseItem(Player player) =>
        player.ownedProjectileCounts[ModContent.ProjectileType<BloodStreamController>()] == 0;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        Vector2 aim = Aim(player, 13f);
        if (Agonized(player, Item))
            Projectile.NewProjectile(source, player.Center, aim, ModContent.ProjectileType<BloodStreamController>(),
                damage, knockback, player.whoAmI);
        else
            for (int i = -1; i <= 1; i++)
                Projectile.NewProjectile(source, player.Center, aim.RotatedBy(i * 0.045f) * (1f - Math.Abs(i) * 0.08f),
                    type, damage, knockback, player.whoAmI);
        return false;
    }

    public override void AddRecipes() => CreateRecipe().AddIngredient(BarType, 8).AddTile(TileID.Anvils).Register();
}
