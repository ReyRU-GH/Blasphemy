using Blasphemy.DamageClass;
using Blasphemy.Players;
using Blasphemy.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Items.Weapons;

public abstract class BloodWeapon : ModItem, BlasphemySystem.ILifeCostItem,
    BlasphemySystem.IPainWeapon, BlasphemySystem.IAgonizedWeapon
{
    public abstract int LifeCost { get; }
    public abstract int RecoveryPercent { get; }
    public abstract int PainGain { get; }
    public override string Texture => "Blasphemy/Assets/Textures/Items/Weapons/" + Name;

    protected void SetWeapon(int damage, int useTime, float shootSpeed = 10f)
    {
        Item.width = 24;
        Item.height = 24;
        Item.damage = damage;
        Item.DamageType = ModContent.GetInstance<SacrificialDamage>();
        Item.useTime = useTime;
        Item.useAnimation = useTime;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 2f;
        Item.noMelee = true;
        Item.autoReuse = true;
        Item.shootSpeed = shootSpeed;
        Item.rare = ItemRarityID.Green;
        Item.value = Item.buyPrice(silver: 20);
    }

    protected static bool Agonized(Player player, Item item) =>
        player.GetModPlayer<BlasphemyPlayer>().IsCurrentAttackAgonized(item);

    protected static Vector2 Aim(Player player, float speed) =>
        (Main.MouseWorld - player.Center).SafeNormalize(new Vector2(player.direction, 0f)) * speed;
}
