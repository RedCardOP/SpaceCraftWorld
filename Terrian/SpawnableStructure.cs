using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface SpawnableStructure
{
    PopulatorTier GetPopulatorTier();

    int GetMinStructuresPerPopulator();
    float GetTargetStructuresPerPopulator();

    //Returns true if successfully populated
    bool Populate(Vector3 pos);
    bool Populate(ChunkCoord cc);
}

public enum PopulatorTier { CHUNK, BIOME}
