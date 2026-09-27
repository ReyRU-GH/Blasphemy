using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ObjectData;

namespace Blasphemy.Tiles.Pylons;

public abstract class BlasphemyPylonTile : ModPylon
{
    protected const int CrystalFrames = 8;
    protected Asset<Texture2D> CrystalTexture;
    protected Asset<Texture2D> CrystalHighlightTexture;
    private Asset<Texture2D> _mapIcon;

    protected abstract int PylonItemType { get; }
    protected abstract Color CrystalColor { get; }

    public override void Load()
    {
        CrystalTexture = ModContent.Request<Texture2D>(Texture + "_Crystal");
        CrystalHighlightTexture = ModContent.Request<Texture2D>(Texture + "_CrystalHighlight");
        _mapIcon = ModContent.Request<Texture2D>(Texture + "_MapIcon");
    }

    public override void SetStaticDefaults()
    {
        Main.tileLighted[Type] = true;
        Main.tileFrameImportant[Type] = true;
        VanillaFallbackOnModDeletion = TileID.TeleportationPylon;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.DrawYOffset = 2;
        TileObjectData.newTile.StyleHorizontal = true;
        TEModdedPylon entity = ModContent.GetInstance<BlasphemyPylonEntity>();
        TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(entity.PlacementPreviewHook_CheckIfCanPlace, 1, 0, true);
        TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(entity.Hook_AfterPlacement, -1, 0, false);
        TileObjectData.addTile(Type);

        TileID.Sets.InteractibleByNPCs[Type] = true;
        TileID.Sets.PreventsSandfall[Type] = true;
        TileID.Sets.AvoidedByMeteorLanding[Type] = true;
        AddToArray(ref TileID.Sets.CountsAsPylon);
        AddMapEntry(CrystalColor, CreateMapEntryName());
    }

    public override NPCShop.Entry GetNPCShopEntry() => null;

    public override void KillMultiTile(int i, int j, int frameX, int frameY) =>
        ModContent.GetInstance<BlasphemyPylonEntity>().Kill(i, j);

    public override void MouseOver(int i, int j)
    {
        Main.LocalPlayer.cursorItemIconEnabled = true;
        Main.LocalPlayer.cursorItemIconID = PylonItemType;
    }

    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
    {
        r = CrystalColor.R / 255f * 0.7f;
        g = CrystalColor.G / 255f * 0.7f;
        b = CrystalColor.B / 255f * 0.7f;
    }

    public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch) =>
        DefaultDrawPylonCrystal(spriteBatch, i, j, CrystalTexture, CrystalHighlightTexture,
            new Vector2(0f, -12f), Color.White * 0.1f, CrystalColor, 4, CrystalFrames);

    public override void DrawMapIcon(ref MapOverlayDrawContext context, ref string mouseOverText,
        TeleportPylonInfo pylonInfo, bool isNearPylon, Color drawColor, float deselectedScale, float selectedScale)
    {
        bool hovered = DefaultDrawMapIcon(ref context, _mapIcon,
            pylonInfo.PositionInTiles.ToVector2() + new Vector2(1.5f, 2f),
            drawColor, deselectedScale, selectedScale);
        DefaultMapClickHandle(hovered, pylonInfo,
            "Mods.Blasphemy.Items." + Name.Replace("Tile", "") + ".DisplayName", ref mouseOverText);
    }
}

public sealed class BlasphemyPylonEntity : TEModdedPylon { }
