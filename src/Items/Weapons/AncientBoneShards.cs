using System;
using Blasphemy.Players;
using Blasphemy.Projectiles.BloodWeapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Blasphemy.Items.Weapons;

public sealed class AncientBoneShards : BloodWeapon
{
    private int _nextCost = 1;
    public override int LifeCost => _nextCost;
    public override int RecoveryPercent => 50;
    public override int PainGain => 1;
    public override void SetDefaults()
    {
        SetWeapon(15, 7, 12f);
        Item.shoot = ModContent.ProjectileType<AncientBoneProjectile>();
    }
    public override void HoldItem(Player player)
    {
        if (!player.controlUseItem)
            _nextCost = 1;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        bool special = Agonized(player, Item);
        int count = Math.Clamp(player.GetModPlayer<BlasphemyPlayer>().LastLifeSpent, 1, 25);
        for (int i = 0; i < count; i++)
            Projectile.NewProjectile(source, player.Center,
                Aim(player, Main.rand.NextFloat(8f, 13f)).RotatedBy(Main.rand.NextFloat(-0.17f, 0.17f)),
                type, damage, knockback, player.whoAmI, special ? 1f : 0f);
        _nextCost = Math.Min(15, _nextCost + 1);
        return false;
    }
}
