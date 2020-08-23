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
    public abstract void Populate();
    public abstract void PopulateTarget(ChunkCoord cc);
    public abstract void PopulateTarget(Vector3 pos);
}

public enum PopulatorTier { CHUNK, BIOME}
