using Blasphemy.Players;
using Blasphemy.Projectiles.BloodWeapons;
using Blasphemy.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Items.Weapons;

public sealed class GlassShard : BloodWeapon, BlasphemySystem.IConditionalActivation,
    BlasphemySystem.IContextualLifeCostItem
{
    private bool _thrustNext;
    public override int LifeCost => 5;
    public override int RecoveryPercent => 30;
    public override int PainGain => 3;
    public bool ActivateOnHitOnly => true;
    public int GetLifeCost(Player player) => player.GetModPlayer<BlasphemyPlayer>().IsCurrentAttackAgonized(Item) ||
        player.GetModPlayer<BlasphemyPlayer>().IsAgonized ? 0 : 5;
    public int GetRecoveryPercent(Player player) => 30;

    public override void SetDefaults()
    {
        SetWeapon(15, 12, 12f);
        Item.noMelee = false;
        Item.shoot = ModContent.ProjectileType<ThrownGlassProjectile>();
        Item.scale = 0.75f;
    }

    public override bool CanUseItem(Player player)
    {
        bool special = player.GetModPlayer<BlasphemyPlayer>().IsAgonized;
        Item.noMelee = special;
        Item.useStyle = special ? ItemUseStyleID.Swing : _thrustNext ? ItemUseStyleID.Rapier : ItemUseStyleID.Swing;
        return true;
    }

    public override void UseAnimation(Player player)
    {
        if (!player.GetModPlayer<BlasphemyPlayer>().CurrentAttackAgonized)
            _thrustNext = !_thrustNext;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer && Agonized(player, Item))
            Projectile.NewProjectile(source, player.Center, Aim(player, 12f), type, damage, knockback, player.whoAmI);
        return false;
    }
}
