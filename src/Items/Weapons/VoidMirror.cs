using Blasphemy.Players;
using Blasphemy.Projectiles.BloodWeapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Items.Weapons;

public sealed class VoidMirror : BloodWeapon
{
    public override int LifeCost => 60;
    public override int RecoveryPercent => 70;
    public override int PainGain => 20;
    public override void SetDefaults()
    {
        SetWeapon(35, 38, 11f);
        Item.shoot = ModContent.ProjectileType<VoidTentacleProjectile>();
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        bool special = Agonized(player, Item);
        int count = 1 + player.GetModPlayer<BlasphemyPlayer>().LastLifeSpent / 10;
        for (int i = 0; i < count; i++)
        {
            float angle = special ? Aim(player, 1f).ToRotation() + Main.rand.NextFloat(-0.28f, 0.28f)
                : Main.rand.NextFloat(MathHelper.TwoPi);
            Projectile.NewProjectile(source, player.Center, angle.ToRotationVector2() * (special ? 16f : 10f),
                type, damage, knockback, player.whoAmI, player.Center.X, player.Center.Y, special ? 1f : 0f);
        }
        return false;
    }
    public override void AddRecipes()
    {
        CreateRecipe().AddIngredient(ItemID.ShadowScale, 10).AddIngredient(ItemID.Obsidian, 30).AddTile(TileID.Anvils).Register();
        CreateRecipe().AddIngredient(ItemID.TissueSample, 10).AddIngredient(ItemID.Obsidian, 30).AddTile(TileID.Anvils).Register();
    }
}
