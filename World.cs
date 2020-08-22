using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public int seed;
    public Transform player;
    public Vector3 spawnPosition;
    public Material material, transparentMaterial;
    public BlockType[] blockTypes;
    public BiomeAttributes biome;
    public WorldGeneration worldGen;
    ChunkCoord _playerLastChunkCoord;


    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    public List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    
    public bool deactivateOutOfViewingDistanceChunks = true;
    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public Queue<ChunkCoord> chunksToDraw = new Queue<ChunkCoord>();


    void Start() {
        Random.InitState(seed);
        spawnPosition = new Vector3(VoxelData.WorldSizeInVoxels / 2f, VoxelData.ChunkHeight - 150f, VoxelData.WorldSizeInVoxels / 2f);
        worldGen = new WorldGeneration(this);
        worldGen.GenerateWorld();
        player.position = spawnPosition;
        playerLastChunkCoord = GetChunkCoord(player.position);
    }

    void Update() {
        if (!playerLastChunkCoord.Equals(GetChunkCoord(player.position)))
        {
            CheckViewDistance();
            playerLastChunkCoord = GetChunkCoord(player.position);
            worldGen.AttemptToSpawnStructures();
        }

        if (chunksToCreate.Count > 0) {
            ChunkCoord cc = chunksToCreate[0];
            chunksToCreate.RemoveAt(0);
            activeChunks.Add(cc);
            chunks[cc.x, cc.z].Init();
        }
        if (chunksToDraw.Count > 0) {
            lock (chunksToDraw) { 
                if (GetChunk(chunksToDraw.Peek()).isEditable)
                    GetChunk(chunksToDraw.Dequeue()).CreateMesh();
            }
        }
           
    }

    //Function currently not in use
    /*IEnumerator CreateChunks()
    {
        isCreatingChunks = true;
        while (chunksToCreate.Count > 0)
        {
            chunks[chunksToCreate[0].x, chunksToCreate[0].z].Init();
            chunksToCreate.RemoveAt(0);
            yield return null;
        }

        isCreatingChunks = false;
    }*/

    //Checks if a given block in global coords is solid
    public bool CheckForVoxel(Vector3 globalPos) {
        ChunkCoord currentChunk = new ChunkCoord(globalPos);
        if (!isVoxelInWorld(globalPos))
            return false;
        if (chunks[currentChunk.x, currentChunk.z] != null && chunks[currentChunk.x, currentChunk.z].isVoxelMapPopulated)
            return chunks[currentChunk.x, currentChunk.z].GetVoxelFromGlobalVector3(globalPos).isSolid;
        return GetBlockType(globalPos).isSolid;

    }

    //Checks if a given block in global coords is solid
    public bool CheckForTransparentVoxel(Vector3 globalPos)
    {
        ChunkCoord currentChunk = new ChunkCoord(globalPos);
        if (!isVoxelInWorld(globalPos))
            return false;
        if (chunks[currentChunk.x, currentChunk.z] != null && chunks[currentChunk.x, currentChunk.z].isVoxelMapPopulated)
        {
            return chunks[currentChunk.x, currentChunk.z].GetVoxelFromGlobalVector3(globalPos).isTransparent;
        }
        return worldGen.GenerateVoxel(globalPos).isTransparent;
    }
    
    void CheckViewDistance()
    {
        ChunkCoord playerCoord = GetChunkCoord(player.position);
        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);
        for (int x = playerCoord.x - VoxelData.ViewDistanceInChunks - 1;
                x <= playerCoord.x + VoxelData.ViewDistanceInChunks + 1; x++){
            for (int z = playerCoord.z - VoxelData.ViewDistanceInChunks - 1;
                    z <= playerCoord.z + VoxelData.ViewDistanceInChunks + 1; z++){
                
                ChunkCoord currentIterationChunkCoord = new ChunkCoord(x, z);
                //Checks if chunk is in the world
                if (isChunkInWorld(currentIterationChunkCoord)){
                    //Checks if chunk has been generated yet
                    if (chunks[x, z] == null){
                        //Checks if chunk is a buffer chunk
                        if (x == playerCoord.x - VoxelData.ViewDistanceInChunks - 1 ||
                            x == playerCoord.x - VoxelData.ViewDistanceInChunks + 1 ||
                            z == playerCoord.z - VoxelData.ViewDistanceInChunks - 1 ||
                            z == playerCoord.z - VoxelData.ViewDistanceInChunks + 1)
                            chunks[x, z] = new Chunk(this, currentIterationChunkCoord, false, false);
                        else
                            chunks[x, z] = new Chunk(this, currentIterationChunkCoord, false, true);
                        chunksToCreate.Add(currentIterationChunkCoord);
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(currentIterationChunkCoord);
                }
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(currentIterationChunkCoord))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }
        if (deactivateOutOfViewingDistanceChunks)
        {
            foreach (ChunkCoord c in previouslyActiveChunks)
            {
                chunks[c.x, c.z].isActive = false;
            }
        }
    }

    public bool isChunkInWorld(ChunkCoord coord) {
        return (coord.x >= 0 && coord.x < VoxelData.WorldSizeInChunks &&
                coord.z >= 0 && coord.z < VoxelData.WorldSizeInChunks);
    }

    public bool isVoxelInWorld(Vector3 globalPos) {
        return (globalPos.x >= 0 && globalPos.x < VoxelData.WorldSizeInVoxels &&
                globalPos.y >= 0 && globalPos.y < VoxelData.ChunkHeight &&
                globalPos.z >= 0 && globalPos.z < VoxelData.WorldSizeInVoxels);
    }
    public BlockType GetBlockType(Vector3 globalPos) {
        return GetChunk(globalPos).GetBlockType(globalPos);
    }
    public ChunkCoord GetChunkCoord(Vector3 globalPos) {
        int x = Mathf.FloorToInt(globalPos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(globalPos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunk(Vector3 globalPos){
        ChunkCoord cc = GetChunkCoord(globalPos);
        if (isChunkInWorld(cc))
            return chunks[cc.x, cc.z];
        else
            return null;
    }

    public Chunk GetChunk(ChunkCoord cc)
    {
        if (isChunkInWorld(cc))
            return chunks[cc.x, cc.z];
        else
            return null;
    }

    public void SetChunk(Chunk chunk, ChunkCoord cc) {
        if (isChunkInWorld(cc))
            chunks[cc.x, cc.z] = chunk;
    }

    public Vector3 GetFlooredVector3(Vector3 pos){
        return new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
    }

    public ChunkCoord playerLastChunkCoord
    {
        get {return _playerLastChunkCoord;}
        set { _playerLastChunkCoord = value; }
    }

}


