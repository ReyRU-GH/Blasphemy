using System;
using System.Collections.Generic;
using Blasphemy.Players;
using Blasphemy.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Blasphemy.Items
{
    public class LifeCostGlobalItem : GlobalItem
    {
        public const string LifeCostDeathMessageKey = "Mods.Blasphemy.Death.LifeCost";

        private int GetLifeCost(Item item, Player player) => item.ModItem is BlasphemySystem.IContextualLifeCostItem contextual
            ? contextual.GetLifeCost(player) : item.ModItem is BlasphemySystem.ILifeCostItem lc ? lc.LifeCost : 0;
        private int GetRecoveryPercent(Item item, Player player) => item.ModItem is BlasphemySystem.IContextualLifeCostItem contextual
            ? contextual.GetRecoveryPercent(player) : item.ModItem is BlasphemySystem.ILifeCostItem lc ? lc.RecoveryPercent : 0;
        
        private bool IsConditional(Item item) => item.ModItem is BlasphemySystem.IConditionalActivation;

        public override bool CanUseItem(Item item, Player player)
        {
            int baseCost = GetLifeCost(item, player);
            if (baseCost <= 0) return base.CanUseItem(item, player);
            if (IsConditional(item)) return base.CanUseItem(item, player);

            var bp = player.GetModPlayer<BlasphemyPlayer>();
         
            int effectiveCost = item.ModItem is BlasphemySystem.IExactAgonizedLifeCost && bp.IsAgonized
                ? baseCost : bp.GetEffectiveLifeCost(baseCost);

            if (player.statLife <= effectiveCost)
            {
                PlayerDeathReason customReason = PlayerDeathReason.ByCustomReason(
                    NetworkText.FromKey(LifeCostDeathMessageKey, player.name)
                );
                player.KillMe(customReason, 9999, 0);
                return false;
            }

            return base.CanUseItem(item, player);
        }

        public override void UseAnimation(Item item, Player player)
        {
           
            if (IsConditional(item))
            {
                if (item.ModItem is BlasphemySystem.IAgonizedWeapon)
                    player.GetModPlayer<BlasphemyPlayer>().BeginAgonizedAttack(item);
                base.UseAnimation(item, player);
                return;
            }

            int baseCost = GetLifeCost(item, player);
            if (baseCost <= 0)
            {
                player.GetModPlayer<BlasphemyPlayer>().LastLifeSpent = 0;
                if (item.ModItem is BlasphemySystem.IAgonizedWeapon)
                    player.GetModPlayer<BlasphemyPlayer>().BeginAgonizedAttack(item);
                base.UseAnimation(item, player);
                return;
            }

            var bp = player.GetModPlayer<BlasphemyPlayer>();
            int effectiveCost = item.ModItem is BlasphemySystem.IExactAgonizedLifeCost && bp.IsAgonized
                ? baseCost : bp.GetEffectiveLifeCost(baseCost);
            int recoveryPercent = GetRecoveryPercent(item, player);
            bp.LastLifeSpent = effectiveCost;

            if (item.ModItem is BlasphemySystem.IAgonizedWeapon)
                bp.BeginAgonizedAttack(item);

           
            player.statLife -= effectiveCost;
            if (player.statLife < 0) player.statLife = 0;
            
            CombatText.NewText(player.getRect(), Color.Red, $"-{effectiveCost}", dramatic: true);

            
            if (recoveryPercent > 0)
            {
                int recoveryGain = (int)Math.Floor(effectiveCost * recoveryPercent / 100f);
                if (recoveryGain > 0)
                {
                    bp.AddRecovery(recoveryGain);
                }
            }

          
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.PlayerHeal, -1, -1, null, player.whoAmI);
            }

            base.UseAnimation(item, player);
        }

        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!IsConditional(item)) return;
            var bp = player.GetModPlayer<BlasphemyPlayer>();
            if (!bp.TryMarkConditionalHit(item)) return;

            int cost = GetLifeCost(item, player);
            if (cost > 0)
            {
                int spent = Math.Min(player.statLife - 1, bp.GetEffectiveLifeCost(cost));
                if (spent > 0)
                {
                    player.statLife -= spent;
                    bp.AddRecovery((int)Math.Floor(spent * GetRecoveryPercent(item, player) / 100f));
                    CombatText.NewText(player.getRect(), Color.Red, $"-{spent}", dramatic: true);
                }
            }

            if (item.ModItem is BlasphemySystem.IPainWeapon painWeapon)
                bp.AddPain(painWeapon.PainGain);
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            int baseCost = GetLifeCost(item, Main.LocalPlayer);
            if (baseCost <= 0) return;

            var bp = Main.LocalPlayer.GetModPlayer<BlasphemyPlayer>();
            int effectiveCost = bp.GetEffectiveLifeCost(baseCost);
            int recoveryPercent = GetRecoveryPercent(item, Main.LocalPlayer);

            
            string baseText = Language.GetTextValue("Mods.Blasphemy.Tooltips.LifeCost", effectiveCost, item.damage);
            
            int damageIndex = tooltips.FindIndex(line => line.Name == "Damage");
            int insertIndex = damageIndex != -1 ? damageIndex + 1 : tooltips.Count;

            tooltips.Insert(insertIndex, new TooltipLine(Mod, "LifeCost", baseText)
            {
                OverrideColor = Color.Red
            });

            if (recoveryPercent > 0)
            {
                int recoveryGain = (int)Math.Floor(effectiveCost * recoveryPercent / 100f);
                string recoveryText = Language.GetTextValue(
                    "Mods.Blasphemy.Tooltips.Recovery",
                    recoveryPercent,
                    recoveryGain
                );

                tooltips.Insert(insertIndex + 1, new TooltipLine(Mod, "Recovery", recoveryText)
                {
                    OverrideColor = Color.LawnGreen
                });
            }

            if (IsConditional(item))
            {
                string condText = Language.GetTextValue("Mods.Blasphemy.Tooltips.Conditional");
                tooltips.Insert(insertIndex + 2, new TooltipLine(Mod, "Conditional", condText)
                {
                    OverrideColor = Color.Gray
                });
            }
        }
    }
}
