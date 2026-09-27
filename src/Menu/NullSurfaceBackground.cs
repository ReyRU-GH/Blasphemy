using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace Blasphemy.Menu;

internal sealed class NullSurfaceBackground : ModSurfaceBackgroundStyle
{
    internal const string BackgroundPath = "Blasphemy/Assets/Textures/Backgrounds/Menu/BG_DT";

    public override void ModifyFarFades(float[] fades, float transitionSpeed)
    {
        for (int i = 0; i < fades.Length; i++)
            fades[i] = Math.Clamp(fades[i] + (i == Slot ? transitionSpeed : -transitionSpeed), 0f, 1f);
    }

    public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b) =>
        BackgroundTextureLoader.GetBackgroundSlot(BackgroundPath);

    public override int ChooseFarTexture() => BackgroundTextureLoader.GetBackgroundSlot(BackgroundPath);
    public override int ChooseMiddleTexture() => BackgroundTextureLoader.GetBackgroundSlot(BackgroundPath);
    public override bool PreDrawCloseBackground(SpriteBatch spriteBatch) => false;
}
