using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SS_Tree : SpawnableStructure
{
    World world;
    Dictionary<ChunkCoord, Queue<VoxelModification>> populationModifications = new Dictionary<ChunkCoord, Queue<VoxelModification>>();
    Queue<VoxelModification> primaryChunkModifications = new Queue<VoxelModification>();
    ChunkCoord ccTarget;

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

    public override void PopulateTarget(Vector3 globalPos)
    {
        ccTarget = world.GetChunkCoord(globalPos);
    }

    public override void PopulateTarget(ChunkCoord cc)
    {
        ccTarget = cc;
    }

    public override void Populate()
    {
        int seed = world.seed ^ ccTarget.x ^ ccTarget.z * 1000;
        System.Random rand = new System.Random(seed);
        int x = rand.Next(0, VoxelData.ChunkWidth);
        int z = rand.Next(0, VoxelData.ChunkWidth);
        Chunk chunk = world.GetChunk(ccTarget);
        int y = chunk.heightMap[x, z] + 1;
        if (!world.blockTypes[chunk.GetBlockType(x, y - 1, z)].blockName.Equals("Grass") &&
            !world.blockTypes[chunk.GetBlockType(x, y - 1, z)].blockName.Equals("Dirt"))
            return;
        //Creates the wood part of tree
        for (int yStart = y; y < yStart + 5; y++) {
            primaryChunkModifications.Enqueue(new VoxelModification(x, y, z, BlockTypes.WOOD));
        }
        //Leaves part of tree
        for (int yLeavesStart = y; y < yLeavesStart + 5; y++)
        {
            for (int i = -3; i < 4; i++)
            {
                for (int j = -3; j < 4; j++)
                {
                    //if (i != 0 || j != 0) {
                        if (chunk.IsVoxelInChunk(x + i, y, z + j))
                            primaryChunkModifications.Enqueue(new VoxelModification(x + i, y, z + j, BlockTypes.LEAVES));
                        else {
                            Vector3 globalPosition = chunk.position + new Vector3(x + i, y, z + j);
                            Chunk neighbourToEdit = world.GetChunk(globalPosition);
                            if (!populationModifications.ContainsKey(neighbourToEdit.coord))
                                populationModifications.Add(neighbourToEdit.coord, new Queue<VoxelModification>());
                            int[] localCoords = neighbourToEdit.GetVoxelLocalCoordsFromGlobalVector3(globalPosition);
                            Queue<VoxelModification> neighbourVoxModQueue;
                            populationModifications.TryGetValue(neighbourToEdit.coord, out neighbourVoxModQueue);
                            neighbourVoxModQueue.Enqueue(new VoxelModification(localCoords[0], localCoords[1], localCoords[2], BlockTypes.LEAVES));
                        }
                    //}
                }
            }
        }
        chunk.EditMultipleBlocks(primaryChunkModifications);

        foreach (ChunkCoord chunkToUpate in populationModifications.Keys)
        {
            Chunk neighbour = world.GetChunk(chunkToUpate);
            Queue<VoxelModification> neighbourVoxModQueue = new Queue<VoxelModification>();
            populationModifications.TryGetValue(neighbour.coord, out neighbourVoxModQueue);
            neighbour.EditMultipleBlocks(neighbourVoxModQueue);
            neighbour.UpdateChunk();
            neighbour.UpdateHeightMap();
        }
        chunk.UpdateChunk();
        //chunk.UpdateSurroundingVoxels(x, z);
        chunk.UpdateHeightMap();
    }
}
