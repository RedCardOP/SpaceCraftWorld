using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

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

    //Objects needed to passed to player
    public Transform hightlightBlock, placeBlock;
    public MeshFilter placeBlockMesh;
    public Text debugOverlay;
    public bool dropItem = true;

    public float gravity = -5f;

    public Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    public List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    
    public bool deactivateOutOfViewingDistanceChunks = true;
    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public Queue<SubchunkCoord> subchunksToDraw = new Queue<SubchunkCoord>();


    void Start() {
        gameObject.AddComponent<EntityManager>();
        Random.InitState(seed);
        spawnPosition = new Vector3(VoxelData.WorldSizeInVoxels / 2f, VoxelData.ChunkHeight - 150f, VoxelData.WorldSizeInVoxels / 2f);
        worldGen = new WorldGeneration(this);
        worldGen.GenerateWorld();
        Player player = new Player(this, hightlightBlock, placeBlock, placeBlockMesh, debugOverlay);
        player.Start();
        player.position = spawnPosition;
        playerLastChunkCoord = GetChunkCoord(player.position);
    }

    void Update() {
        if (!playerLastChunkCoord.Equals(GetChunkCoord(player.position))){
            CheckViewDistance();
            playerLastChunkCoord = GetChunkCoord(player.position);
            worldGen.AttemptToSpawnStructures();
        }

        if (chunksToCreate.Count > 0){
            ChunkCoord cc = chunksToCreate[0];
            chunksToCreate.RemoveAt(0);
            activeChunks.Add(cc);
            chunks[cc.x, cc.z].Init();
        }
        if (subchunksToDraw.Count > 0){
            lock (subchunksToDraw) {
                for (int i = 0; i < PerformanceSettings.subchunksToDrawPerFrame; i++){
                    if (subchunksToDraw.Count == 0) break;
                    if (GetSubchunk(subchunksToDraw.Peek()).isEditable){
                        GetSubchunk(subchunksToDraw.Dequeue()).CreateMesh();
                    }
                }
            }
        }
    }

    //Checks if a given block in global coords is solid
    public bool CheckForVoxel(Vector3 globalPos) {
        ChunkCoord currentChunk = new ChunkCoord(globalPos);
        if (!isVoxelInWorld(globalPos))
            return false;
        if (chunks[currentChunk.x, currentChunk.z] != null && chunks[currentChunk.x, currentChunk.z].isVoxelMapPopulated)
            return chunks[currentChunk.x, currentChunk.z].GetBlock(globalPos).isSolid;
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
            return chunks[currentChunk.x, currentChunk.z].GetBlock(globalPos).isTransparent;
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
    public Chunk GetChunk(ChunkCoord cc) {
        if (isChunkInWorld(cc))
            return chunks[cc.x, cc.z];
        else
            return null;
    }

    public Subchunk GetSubchunk(SubchunkCoord sc) {
        Chunk superChunk = GetChunk(sc.superChunkCoord);
        if (superChunk != null)
            return superChunk.GetSubchunk(sc.subchunkIndex);
        else
            return null;
    }

    public SubchunkCoord GetSubchunkCoord(Vector3 globalPos) {
        ChunkCoord superChunk = GetChunkCoord(globalPos);
        byte subchunkIndex = Subchunk.GetSubchunkIndex(Mathf.FloorToInt(globalPos.y));
        return new SubchunkCoord(superChunk, subchunkIndex);
    }

    public void SetChunk(Chunk chunk, ChunkCoord cc) {
        if (isChunkInWorld(cc))
            chunks[cc.x, cc.z] = chunk;
    }

    public static Vector3 GetFlooredVector3(Vector3 pos){
        return new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
    }

    public ChunkCoord playerLastChunkCoord
    {
        get {return _playerLastChunkCoord;}
        set { _playerLastChunkCoord = value; }
    }

}
