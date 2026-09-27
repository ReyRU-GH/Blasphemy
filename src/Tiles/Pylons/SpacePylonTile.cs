using Blasphemy.Items.Pylons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Blasphemy.Tiles.Pylons;

public sealed class SpacePylonTile : BlasphemyPylonTile
{
    public override string Texture => "Blasphemy/Assets/Textures/Tiles/Pylons/SpacePylonTile";
    protected override int PylonItemType => ModContent.ItemType<SpacePylon>();
    protected override Color CrystalColor => Color.DeepSkyBlue;

    public override bool ValidTeleportCheck_BiomeRequirements(TeleportPylonInfo pylonInfo, SceneMetrics sceneData) =>
        pylonInfo.PositionInTiles.Y < Main.worldSurface * 0.45;

}
