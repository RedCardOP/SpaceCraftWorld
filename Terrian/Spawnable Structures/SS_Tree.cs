using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SS_Tree : SpawnableStructure
{
    World world;
    public SS_Tree(World world) : base(PopulatorTier.CHUNK) {
        this.world = world;
    }
    public override int GetMinStructuresPerPopulator()
    {
        throw new System.NotImplementedException();
    }

    public override float GetTargetStructuresPerPopulator()
    {
        throw new System.NotImplementedException();
    }

    public override bool Populate(Vector3 pos)
    {
        throw new System.NotImplementedException();
    }

    public override bool Populate(ChunkCoord cc)
    {
        int seed = world.seed ^ cc.x ^ cc.z * 1000;
        System.Random rand = new System.Random(seed);
        int x = rand.Next(0, VoxelData.ChunkWidth);
        int z = rand.Next(0, VoxelData.ChunkWidth);
        //This is just temporary to decrease the load of trees spawned
        int toSpawn = rand.Next(0, 10);
        if (toSpawn != 0)
            return false;
        Chunk chunk = world.GetChunk(cc);
        List<ChunkCoord> neighboursToUpdate = new List<ChunkCoord>();
        int y = chunk.heightMap[x, z] + 1;
        //Checks if block underneath is dirt or grass
        if (!world.blockTypes[chunk.GetBlockType(x, y - 1, z)].blockName.Equals("Grass") &&
            !world.blockTypes[chunk.GetBlockType(x, y - 1, z)].blockName.Equals("Dirt"))
            return false;
        //Creates the wood part of tree
        for (int i = 0; i < 10; i++) {
            chunk.EditVoxel(x, y + i, z, BlockTypes.WOOD);
        }
        //Leaves part of tree
        for (int yLeaves = y + 8; yLeaves < y + 11; yLeaves++)
        {
            for (int i = -4; i < 5; i++)
            {
                for (int j = -4; j < 5; j++)
                {
                    if (i != 0 || j != 0) {
                        //If the leave being placed down is in the same chunk as the wood part
                        if (chunk.IsVoxelInChunk(x + i, yLeaves, z + j))
                            chunk.EditVoxel(x + i, yLeaves, z + j, BlockTypes.LEAVES);
                        else {
                            Vector3 globalPosition = chunk.position + new Vector3(x + i, yLeaves, z + j);
                            Chunk neighbourToEdit = world.GetChunk(globalPosition);
                            int[] localCoords = neighbourToEdit.GetVoxelLocalCoordsFromGlobalVector3(globalPosition);
                            neighbourToEdit.EditVoxel(localCoords[0], localCoords[1], localCoords[2], BlockTypes.LEAVES);
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
