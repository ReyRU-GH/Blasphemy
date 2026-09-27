using Blasphemy.Tiles.Pylons;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Items.Pylons;

public sealed class CorruptPylon : ModItem
{
    public override string Texture => "Blasphemy/Assets/Textures/Items/Pylons/CorruptPylon";

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<CorruptPylonTile>());
        Item.width = 32;
        Item.height = 42;
        Item.value = Item.buyPrice(gold: 25);
        Item.rare = ItemRarityID.Blue;
    }
}
