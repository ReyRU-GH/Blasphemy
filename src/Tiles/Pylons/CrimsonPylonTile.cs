using Blasphemy.Items.Pylons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Blasphemy.Tiles.Pylons;

public sealed class CrimsonPylonTile : BlasphemyPylonTile
{
    public override string Texture => "Blasphemy/Assets/Textures/Tiles/Pylons/CrimsonPylonTile";
    protected override int PylonItemType => ModContent.ItemType<CrimsonPylon>();
    protected override Color CrystalColor => Color.Red;
    public override bool ValidTeleportCheck_NPCCount(TeleportPylonInfo pylonInfo, int defaultNecessaryNPCCount) => true;
    public override bool ValidTeleportCheck_BiomeRequirements(TeleportPylonInfo pylonInfo, SceneMetrics sceneData) =>
        sceneData.EnoughTilesForCrimson;
}
