using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Blasphemy.Items
{
    /// <summary>
    /// A custom implementation of tracking a projectile's item source, even if it's in the chain
    /// like projectile > projectile > item, it will track that item too.
    /// </summary>
    public class GlobalProjectileTracker : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public Item ParentItem;
        
        public override void OnSpawn(Projectile projectile, IEntitySource source) {
            if (source is EntitySource_ItemUse itemSource) {
                ParentItem = itemSource.Item;
            }
            else if (source is EntitySource_Parent parentSource && parentSource.Entity is Projectile parentProj) {
                if (parentProj.TryGetGlobalProjectile<GlobalProjectileTracker>(out var parentTracker)) {
                    ParentItem = parentTracker.ParentItem;
                }
            }
        }
    }
}