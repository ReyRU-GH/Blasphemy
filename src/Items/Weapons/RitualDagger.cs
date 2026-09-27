using System;
using Blasphemy.Players;
using Blasphemy.Projectiles.BloodWeapons;
using Blasphemy.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Blasphemy.Items.Weapons;

public sealed class RitualDagger : BloodWeapon, BlasphemySystem.IContextualLifeCostItem,
    BlasphemySystem.IExactAgonizedLifeCost
{
    public override int LifeCost => 35;
    public override int RecoveryPercent => 40;
    public override int PainGain => 20;
    public int GetLifeCost(Player player) => player.GetModPlayer<BlasphemyPlayer>().IsAgonized
        ? Math.Max(0, player.statLife - 1) : 35;
    public int GetRecoveryPercent(Player player) => player.GetModPlayer<BlasphemyPlayer>().IsAgonized ? 90 : 40;

    public override void SetDefaults()
    {
        SetWeapon(1, 23);
        Item.shoot = ModContent.ProjectileType<RitualPulseProjectile>();
        Item.value = Item.buyPrice(gold: 20);
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        int spent = player.GetModPlayer<BlasphemyPlayer>().LastLifeSpent;
        int rawDamage = 1 + spent * 2;
        int finalDamage = Math.Max(1, (int)player.GetTotalDamage(Item.DamageType).ApplyTo(rawDamage));
        Rectangle visible = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.active && !npc.friendly && npc.CanBeChasedBy() && visible.Intersects(npc.Hitbox))
                Projectile.NewProjectile(source, npc.Center, Vector2.Zero, type, finalDamage, 0f, player.whoAmI, npc.whoAmI + 1);
        }
        if (Agonized(player, Item))
            for (int i = 0; i < Player.MaxBuffs; i++)
                if (player.buffTime[i] > 0 && Main.debuff[player.buffType[i]])
                    player.DelBuff(i--);
        return false;
    }
}
