using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Blasphemy.Systems;

public sealed class FossilEncounterSpawns : GlobalNPC
{
    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        if (FossilEncounterSystem.IsEncounterNpcAlive())
            pool.Clear();
    }

    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (!FossilEncounterSystem.IsEncounterNpcAlive())
            return;

        if (spawnRate < 1_000_000)
            spawnRate = 1_000_000;
        maxSpawns = 0;
    }
}
