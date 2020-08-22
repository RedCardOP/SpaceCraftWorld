using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGeneration
{
    World world;
    List<ChunkCoord> chunksToSpawnStructures = new List<ChunkCoord>();

    public WorldGeneration(World world) {
        this.world = world;
    }

    public void GenerateWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; x <= (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; z <= (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; z++)
            {
                if (x >= 0 && z >= 0)
                {
                    ChunkCoord cc = new ChunkCoord(x, z);
                    world.SetChunk(new Chunk(world, cc, true, true), cc);
                    world.activeChunks.Add(cc);
                }
            }
        }
        //AttemptToSpawnStructures();
    }

    public BlockType GenerateVoxel(Vector3 pos)
    {
        // ABSOLUTE PASS
        // If out of world, air
        if (!world.isVoxelInWorld(pos))
            return BlockTypes.AIR;
        // If bottom block, EOTW
        else if (pos.y == 0)
            return BlockTypes.EOTW;

        BlockType voxelToReturn;
        //Basic terrain sculpting
        int terrainHeight = Mathf.FloorToInt(world.biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z),
            0, world.biome.terrainScale)) + world.biome.solidGroundHeight;

        if (pos.y == terrainHeight)
            voxelToReturn = BlockTypes.GRASS;
        else if (pos.y < terrainHeight && pos.y > terrainHeight - 3)
            voxelToReturn = BlockTypes.DIRT;
        else if (pos.y < terrainHeight)
            voxelToReturn = BlockTypes.STONE;
        else
            voxelToReturn = BlockTypes.AIR;

        //Ore pass
        if (voxelToReturn == BlockTypes.STONE)
        {
            foreach (Lode lode in world.biome.lodes)
            {
                if (pos.y > lode.minHeight && pos.y < lode.maxHeight)
                {
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelToReturn = lode.blockType;
                }
            }
        }

        return voxelToReturn;

    }

    public bool AreNeighboursPopulated(ChunkCoord cc)
    {
        return (world.GetChunk(new ChunkCoord(cc.x - 1, cc.z)) != null && world.GetChunk(new ChunkCoord(cc.x - 1, cc.z)).isVoxelMapPopulated &&
                world.GetChunk(new ChunkCoord(cc.x - 1, cc.z - 1)) != null && world.GetChunk(new ChunkCoord(cc.x - 1, cc.z - 1)).isVoxelMapPopulated &&
                world.GetChunk(new ChunkCoord(cc.x, cc.z - 1)) != null && world.GetChunk(new ChunkCoord(cc.x, cc.z - 1)).isVoxelMapPopulated &&
                world.GetChunk(new ChunkCoord(cc.x + 1, cc.z - 1)) != null && world.GetChunk(new ChunkCoord(cc.x + 1, cc.z - 1)).isVoxelMapPopulated &&
                world.GetChunk(new ChunkCoord(cc.x + 1, cc.z)) != null && world.GetChunk(new ChunkCoord(cc.x + 1, cc.z)).isVoxelMapPopulated &&
                world.GetChunk(new ChunkCoord(cc.x + 1, cc.z + 1)) != null && world.GetChunk(new ChunkCoord(cc.x + 1, cc.z + 1)).isVoxelMapPopulated &&
                world.GetChunk(new ChunkCoord(cc.x, cc.z + 1)) != null && world.GetChunk(new ChunkCoord(cc.x, cc.z + 1)).isVoxelMapPopulated &&
                world.GetChunk(new ChunkCoord(cc.x - 1, cc.z + 1)) != null && world.GetChunk(new ChunkCoord(cc.x - 1, cc.z + 1)).isVoxelMapPopulated);
    }

    public void AttemptToSpawnStructures()
    {
        List<int> chunksToRemoveFromList = new List<int>();
        for (int i = 0; i < chunksToSpawnStructures.Count; i++)
        {
            ChunkCoord cc = chunksToSpawnStructures[i];
            //Checks to see if chunk we are attempting spawning on is in the view distance. If not, skip
            if (Mathf.Abs(world.playerLastChunkCoord.x - cc.x) > VoxelData.ViewDistanceInChunks &&
                Mathf.Abs(world.playerLastChunkCoord.z - cc.z) > VoxelData.ViewDistanceInChunks)
                continue;
            //Check neighbours to see if exist and voxels are populated
            if (AreNeighboursPopulated(cc))
            {
                world.GetChunk(cc).PopulateSpawnableStructures();
                chunksToRemoveFromList.Add(i);
            }
        }
        // Goes in reverse order so as to not disturb index. Removes chunks that have been populated
        for (int j = chunksToRemoveFromList.Count - 1; j >= 0; j--)
        {
            chunksToSpawnStructures.RemoveAt(j);
        }
    }

    public void GenerateSpawnableStructures(ChunkCoord cc)
    {
        if (AreNeighboursPopulated(cc))
            world.GetChunk(cc).PopulateSpawnableStructures();
        else
        {
            lock (chunksToSpawnStructures){
                chunksToSpawnStructures.Add(cc);
            }
        }

    }

}
