using Blasphemy.Players;
using Terraria;
using Terraria.ModLoader;

namespace Blasphemy.Systems
{
    public class BlasphemySystem : ModSystem
    {
        public static ModKeybind TestPainKey { get; private set; }
        public static ModKeybind TestRemovePainKey { get; private set; }
        
        public interface ILifeCostItem
        {
            int LifeCost { get; }
            int RecoveryPercent { get; }
        }
        public interface IPainWeapon
        {
            /// <summary>
            /// Количество очков боли, получаемое при использовании оружия
            /// </summary>
            int PainGain { get; }
        }
        public interface IConditionalActivation
        {
            /// <summary>
            /// Если true, статы применяются только при попадании
            /// Если false, статы применяются при каждом взмахе
            /// </summary>
            bool ActivateOnHitOnly { get; }
        }

        public override void Load()
        {
            TestPainKey = KeybindLoader.RegisterKeybind(Mod, "Add Pain (Test)", "F");
            TestRemovePainKey = KeybindLoader.RegisterKeybind(Mod, "Remove Pain (Test)", "G");
        }

        public override void Unload()
        {
            TestPainKey = null;
            TestRemovePainKey = null;
        }

        public override void PostUpdateEverything()
        {
            if (Main.gameMenu) return;

            var bp = Main.LocalPlayer.GetModPlayer<BlasphemyPlayer>();

            if (TestPainKey.JustPressed)
            {
                bp.AddPain(10);
            }
            if (TestRemovePainKey.JustPressed)
            {
            }
        }
    }
}