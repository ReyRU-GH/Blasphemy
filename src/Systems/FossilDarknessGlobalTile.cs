using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Systems;

public sealed class FossilDarknessGlobalTile : GlobalTile
{
    public override void ModifyLight(int i, int j, int type, ref float r, ref float g, ref float b)
    {
        if (Main.dedServ || !FossilEncounterSystem.ShouldApplyDarknessToLocalPlayer())
            return;

        if (!TileID.Sets.Torch[type])
            return;

        r *= 0.65f;
        g *= 0.65f;
        b *= 0.65f;
    }
}
