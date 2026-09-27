using Terraria.ID;

namespace Blasphemy.Items.Weapons;

public sealed class GoldenBlade : MetalBlade
{
    protected override int NormalCost => 5;
    protected override int AgonyCost => 15;
    protected override int BarType => ItemID.GoldBar;
}
