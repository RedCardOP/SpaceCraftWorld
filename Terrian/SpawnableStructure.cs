using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpawnableStructure
{
    PopulatorTier popTier;
    public SpawnableStructure(PopulatorTier popTier) {
        this.popTier = popTier;
    }
    public PopulatorTier GetPopulatorTier() {
        return popTier;
    }

    public abstract int GetMinStructuresPerPopulator();
    public abstract float GetTargetStructuresPerPopulator();

    //Returns true if successfully populated
    public abstract bool Populate(Vector3 pos);
    public abstract bool Populate(ChunkCoord cc);
}

public enum PopulatorTier { CHUNK, BIOME}
