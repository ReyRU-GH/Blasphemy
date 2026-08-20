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

        private int GetLifeCost(Item item) => item.ModItem is BlasphemySystem.ILifeCostItem lc ? lc.LifeCost : 0;
        private int GetRecoveryPercent(Item item) => item.ModItem is BlasphemySystem.ILifeCostItem lc ? lc.RecoveryPercent : 0;
        
        private bool IsConditional(Item item) => item.ModItem is BlasphemySystem.IConditionalActivation;

        public override bool CanUseItem(Item item, Player player)
        {
            int baseCost = GetLifeCost(item);
            if (baseCost <= 0) return base.CanUseItem(item, player);

            var bp = player.GetModPlayer<BlasphemyPlayer>();
         
            int effectiveCost = bp.GetEffectiveLifeCost(baseCost);

            if (player.statLife <= effectiveCost)
            {
                PlayerDeathReason customReason = PlayerDeathReason.ByCustomReason(
                    NetworkText.FromKey(LifeCostDeathMessageKey, player.name)
                );
                player.KillMe(customReason, 9999, 0, false);
                return false;
            }

            return base.CanUseItem(item, player);
        }

        public override void UseAnimation(Item item, Player player)
        {
           
            if (IsConditional(item))
            {
                base.UseAnimation(item, player);
                return;
            }

            int baseCost = GetLifeCost(item);
            if (baseCost <= 0)
            {
                base.UseAnimation(item, player);
                return;
            }

            var bp = player.GetModPlayer<BlasphemyPlayer>();
            int effectiveCost = bp.GetEffectiveLifeCost(baseCost);

           
            player.statLife -= effectiveCost;
            if (player.statLife < 0) player.statLife = 0;
            
            CombatText.NewText(player.getRect(), Color.Red, $"-{effectiveCost}", dramatic: true);

            
            int recoveryPercent = GetRecoveryPercent(item);
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

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            int baseCost = GetLifeCost(item);
            if (baseCost <= 0) return;

            var bp = Main.LocalPlayer.GetModPlayer<BlasphemyPlayer>();
            int effectiveCost = bp.GetEffectiveLifeCost(baseCost);
            int recoveryPercent = GetRecoveryPercent(item);

            
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