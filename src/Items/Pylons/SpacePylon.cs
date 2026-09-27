using Blasphemy.Tiles.Pylons;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Items.Pylons;

public sealed class SpacePylon : ModItem
{
    public override string Texture => "Blasphemy/Assets/Textures/Items/Pylons/SpacePylon";

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<SpacePylonTile>());
        Item.width = 32;
        Item.height = 42;
        Item.value = Item.buyPrice(gold: 10);
        Item.rare = ItemRarityID.Blue;
    }
}
