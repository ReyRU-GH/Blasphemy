using Terraria.ID;

namespace Blasphemy.Items.Weapons;

public sealed class PlatinumBlade : MetalBlade
{
    protected override int NormalCost => 4;
    protected override int AgonyCost => 14;
    protected override int BarType => ItemID.PlatinumBar;
}
