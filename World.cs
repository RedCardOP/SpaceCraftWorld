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


    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    ChunkCoord playerLastChunkCoord;
    public bool deactivateOutOfViewingDistanceChunks = true;
    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    List<ChunkCoord> chunksToSpawnStructures = new List<ChunkCoord>();
    private bool isCreatingChunks;


    void Start() {
        Random.InitState(seed);
        spawnPosition = new Vector3(VoxelData.WorldSizeInVoxels / 2f, VoxelData.ChunkHeight - 150f, VoxelData.WorldSizeInVoxels / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoord(player.position);
    }

    void Update() {
        if (!playerLastChunkCoord.Equals(GetChunkCoord(player.position)))
        {
            CheckViewDistance();
            playerLastChunkCoord = GetChunkCoord(player.position);
            AttemptToSpawnStructures();
        }

        if (chunksToCreate.Count > 0 && !isCreatingChunks)
            StartCoroutine("CreateChunks");

    }

    void GenerateWorld() {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; x <= (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; x++) {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; z <= (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; z++) {
                if (x >= 0 && z >= 0) {
                    chunks[x, z] = new Chunk(this, new ChunkCoord(x, z), true, true);
                    activeChunks.Add(new ChunkCoord(x, z));
                }
            }
        }
        player.position = spawnPosition;
        //AttemptToSpawnStructures();
    }

    public void GenerateSpawnableStructures(ChunkCoord cc) { 
        if (AreNeighboursPopulated(cc))
                GetChunk(cc).PopulateSpawnableStructures();
        else {
            chunksToSpawnStructures.Add(cc);
        }

    }

    public void AttemptToSpawnStructures()
    {
        List<int> chunksToRemoveFromList = new List<int>();
        for (int i = 0; i < chunksToSpawnStructures.Count; i++) {
            ChunkCoord cc = chunksToSpawnStructures[i];
            //Checks to see if chunk we are attempting spawning on is in the view distance. If not, skip
            if (Mathf.Abs(playerLastChunkCoord.x - cc.x) > VoxelData.ViewDistanceInChunks &&
                Mathf.Abs(playerLastChunkCoord.z - cc.z) > VoxelData.ViewDistanceInChunks)
                continue;
            //Check neighbours to see if exist and voxels are populated
            if (AreNeighboursPopulated(cc)) {
                    GetChunk(cc).PopulateSpawnableStructures();
                    chunksToRemoveFromList.Add(i);
            }
        }
        // Goes in reverse order so as to not disturb index. Removes chunks that have been populated
        for (int j = chunksToRemoveFromList.Count - 1; j >= 0; j--) {
            chunksToSpawnStructures.RemoveAt(j);
        }
    }

    IEnumerator CreateChunks()
    {
        isCreatingChunks = true;
        while (chunksToCreate.Count > 0)
        {
            chunks[chunksToCreate[0].x, chunksToCreate[0].z].Init();
            chunksToCreate.RemoveAt(0);
            yield return null;
        }

        isCreatingChunks = false;
    }

    //Checks if a given block in global coords is solid
    public bool CheckForVoxel(Vector3 pos) {
        ChunkCoord currentChunk = new ChunkCoord(pos);
        if (!isVoxelInWorld(pos))
            return false;
        if (chunks[currentChunk.x, currentChunk.z] != null && chunks[currentChunk.x, currentChunk.z].isVoxelMapPopulated)
            return blockTypes[chunks[currentChunk.x, currentChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        return blockTypes[GetVoxel(pos)].isSolid;

    }

    //Checks if a given block in global coords is solid
    public bool CheckForTransparentVoxel(Vector3 pos)
    {
        ChunkCoord currentChunk = new ChunkCoord(pos);
        if (!isVoxelInWorld(pos))
            return false;
        if (chunks[currentChunk.x, currentChunk.z] != null && chunks[currentChunk.x, currentChunk.z].isVoxelMapPopulated)
            return blockTypes[chunks[currentChunk.x, currentChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;
        return blockTypes[GetVoxel(pos)].isTransparent;

    }

    public byte GetVoxel(Vector3 pos) {
        // ABSOLUTE PASS
        // If out of world, air
        if (!isVoxelInWorld(pos))
            return 0;
        // If bottom block, EOTW
        else if (pos.y == 0)
            return 5;

        byte voxelToReturn;
        //Basic terrain sculpting
        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z),
            0, biome.terrainScale)) + biome.solidGroundHeight;

        if (pos.y == terrainHeight)
            voxelToReturn = 1;
        else if (pos.y < terrainHeight && pos.y > terrainHeight - 3)
            voxelToReturn = 2;
        else if (pos.y < terrainHeight)
            voxelToReturn = 3;
        else
            voxelToReturn = 0;

        //Ore pass
        if (voxelToReturn == 3) {
            foreach (Lode lode in biome.lodes) {
                if (pos.y > lode.minHeight && pos.y < lode.maxHeight) {
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelToReturn = lode.blockID;
                }
            }
        }

        return voxelToReturn;


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

    public bool isVoxelInWorld(Vector3 pos) {
        return (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels &&
                pos.y >= 0 && pos.y < VoxelData.ChunkHeight &&
                pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels);
    }

    public ChunkCoord GetChunkCoord(Vector3 pos) {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunk(Vector3 pos){
        ChunkCoord cc = GetChunkCoord(pos);
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

    public Vector3 GetFlooredVector3(Vector3 pos){
        return new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
    }

    public bool AreNeighboursPopulated(ChunkCoord cc) { 
        return (GetChunk(new ChunkCoord(cc.x - 1, cc.z)) != null && GetChunk(new ChunkCoord(cc.x - 1, cc.z)).isVoxelMapPopulated &&
                GetChunk(new ChunkCoord(cc.x - 1, cc.z - 1)) != null && GetChunk(new ChunkCoord(cc.x - 1, cc.z - 1)).isVoxelMapPopulated &&
                GetChunk(new ChunkCoord(cc.x, cc.z - 1)) != null && GetChunk(new ChunkCoord(cc.x, cc.z - 1)).isVoxelMapPopulated &&
                GetChunk(new ChunkCoord(cc.x + 1, cc.z - 1)) != null && GetChunk(new ChunkCoord(cc.x + 1, cc.z - 1)).isVoxelMapPopulated &&
                GetChunk(new ChunkCoord(cc.x + 1, cc.z)) != null && GetChunk(new ChunkCoord(cc.x + 1, cc.z)).isVoxelMapPopulated &&
                GetChunk(new ChunkCoord(cc.x + 1, cc.z + 1)) != null && GetChunk(new ChunkCoord(cc.x + 1, cc.z + 1)).isVoxelMapPopulated &&
                GetChunk(new ChunkCoord(cc.x, cc.z + 1)) != null && GetChunk(new ChunkCoord(cc.x, cc.z + 1)).isVoxelMapPopulated &&
                GetChunk(new ChunkCoord(cc.x - 1, cc.z + 1)) != null && GetChunk(new ChunkCoord(cc.x - 1, cc.z + 1)).isVoxelMapPopulated);
    }

}

[System.Serializable]
public class BlockType {
    public string blockName;
    public bool isSolid;
    public bool isTransparent;

    [Header("Texture ID Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;



    public int GetTextureID(int faceIndex) {

        switch (faceIndex) {

            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Texture is fucked");
                return 0;

        }

    }
    
}
