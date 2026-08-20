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
        public override void UseAnimation(Item item, Player player)
        {

            if (item.ModItem is BlasphemySystem.IConditionalActivation) return;

            if (item.ModItem is BlasphemySystem.IPainWeapon pw && pw.PainGain > 0)
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