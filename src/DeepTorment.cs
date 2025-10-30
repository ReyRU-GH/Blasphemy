using Terraria.ModLoader;

namespace DeepTorment
{
    public class DeepTorment : Mod
    {
        public static DeepTorment Instance { get; private set; }
        
        public DeepTorment()
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
