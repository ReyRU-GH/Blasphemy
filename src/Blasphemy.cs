using Terraria.ModLoader;

namespace Blasphemy
{
    public class Blasphemy : Mod
    {
        public static Blasphemy Instance { get; private set; }
        
        public Blasphemy()
        {
            Instance = this;
        }
        
        public override void Load()
        {

        }
        
        public override void Unload()
        {
            Instance = null;
        }
    }
}
