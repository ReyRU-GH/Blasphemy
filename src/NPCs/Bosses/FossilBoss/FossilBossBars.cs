using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Blasphemy.NPCs.Bosses.FossilBoss;
public abstract class FossilBossBar : ModBossBar
{
    private const string AssetRoot = "Blasphemy/Assets/Textures/Bosses/Fossil/";

    protected abstract string HeadName { get; }
    protected abstract Color FillColor { get; }

    public override string Texture => AssetRoot + "BossBar";

    public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
    {
        iconFrame = new Rectangle(3, 2, 26, 28);
        return ModContent.Request<Texture2D>(AssetRoot + HeadName);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
    {
        drawParams.BarColor = FillColor;
        return true;
    }
}

public sealed class FossilSnakeBossBar : FossilBossBar
{
    protected override string HeadName => "SnakeHead";
    protected override Color FillColor => new(174, 129, 93);
}

public sealed class FossilLizardBossBar : FossilBossBar
{
    protected override string HeadName => "LizardHead";
    protected override Color FillColor => new(203, 171, 96);
}

public sealed class FossilSharkBossBar : FossilBossBar
{
    protected override string HeadName => "SharkHead";
    protected override Color FillColor => new(120, 153, 164);
}
