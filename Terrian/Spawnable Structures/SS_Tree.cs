using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SS_Tree : SpawnableStructure
{
    World world;
    public SS_Tree(World world) {
        this.world = world;
    }
    public int GetMinStructuresPerPopulator()
    {
        throw new System.NotImplementedException();
    }

    public PopulatorTier GetPopulatorTier()
    {
        throw new System.NotImplementedException();
    }

    public float GetTargetStructuresPerPopulator()
    {
        throw new System.NotImplementedException();
    }

    public bool Populate(Vector3 pos)
    {
        throw new System.NotImplementedException();
    }

    public bool Populate(ChunkCoord cc)
    {
        int seed = world.seed ^ cc.x ^ cc.z * 1000;
        Random.InitState(seed);
        int x = Random.Range(0, VoxelData.ChunkWidth);
        int z = Random.Range(0, VoxelData.ChunkWidth);
        Chunk chunk = world.GetChunk(cc);
        List<ChunkCoord> neighboursToUpdate = new List<ChunkCoord>();
        int y = chunk.heightMap[x, z] + 1;
        if (!world.blockTypes[chunk.GetBlockType(x, y - 1, z)].blockName.Equals("Grass") &&
            !world.blockTypes[chunk.GetBlockType(x, y - 1, z)].blockName.Equals("Dirt"))
            return false;
        for (int i = 0; i < 10; i++)
        {
            chunk.EditVoxelWithoutMeshUpdate(x, y + i, z, 4);
        }
        for (int yLeaves = y + 8; yLeaves < y + 11; yLeaves++)
        {
            for (int i = -4; i < 5; i++)
            {
                for (int j = -4; j < 5; j++)
                {
                    if (i != 0 || j != 0) {
                        if (chunk.IsVoxelInChunk(x + i, yLeaves, z + j))
                            chunk.EditVoxelWithoutMeshUpdate(x + i, yLeaves, z + j, 7);
                        else {
                            Vector3 globalPosition = chunk.position + new Vector3(x + i, yLeaves, z + j);
                            Chunk neighbourToEdit = world.GetChunk(globalPosition);
                            int[] localCoords = neighbourToEdit.GetVoxelLocalCoordsFromGlobalVector3(globalPosition);
                            neighbourToEdit.EditVoxelWithoutMeshUpdate(localCoords[0], localCoords[1], localCoords[2], 7);
                            if (!neighboursToUpdate.Contains(neighbourToEdit.coord))
                                neighboursToUpdate.Add(neighbourToEdit.coord);
                        }
                    }
                }
            }
        }
        chunk.UpdateChunk();
        chunk.UpdateSurroundingVoxels(x, z);
        chunk.UpdateHeightMap();
        foreach(ChunkCoord chunkToUpate in neighboursToUpdate) {
            Chunk neighbour = world.GetChunk(chunkToUpate);
            neighbour.UpdateChunk();
            neighbour.UpdateHeightMap();
        }
          
        return true;

    }
}
