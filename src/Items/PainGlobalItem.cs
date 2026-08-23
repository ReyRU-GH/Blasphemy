using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Blasphemy.Systems;
using Blasphemy.Players;

namespace Blasphemy.Items
{
    public class PainGlobalItem : GlobalItem
    {
        public override bool? UseItem(Item item, Player player)
        {
            if (item.ModItem is BlasphemySystem.IPainWeapon pw && pw.PainGain > 0 && player.itemAnimation == player.itemAnimationMax && item.ModItem is not BlasphemySystem.IConditionalActivation)
            {
                var bp = player.GetModPlayer<BlasphemyPlayer>();
                bp.AddPain(pw.PainGain);
                
                bp.LastWeaponUsed = item;
            }

            return true;
        }

        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (item.ModItem is BlasphemySystem.IPainWeapon pw && pw.PainGain > 0 && player.itemAnimation == player.itemAnimationMax && item.ModItem is BlasphemySystem.IConditionalActivation)
            {
                var bp = player.GetModPlayer<BlasphemyPlayer>();
                bp.AddPain(pw.PainGain);
                
                bp.LastWeaponUsed = item;
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.ModItem is BlasphemySystem.IPainWeapon pw && pw.PainGain > 0)
            {
                string text = Language.GetTextValue("Mods.Blasphemy.Tooltips.PainGain", pw.PainGain);
                int damageIndex = tooltips.FindIndex(l => l.Name == "Damage");
                int insertIndex = damageIndex != -1 ? damageIndex + 1 : tooltips.Count;

                tooltips.Insert(insertIndex, new TooltipLine(Mod, "PainGain", text) 
                { 
                    OverrideColor = new Color(200, 100, 255) 
                });
            }
        }
    }
}