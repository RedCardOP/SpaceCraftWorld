using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Chunk
{
    public GameObject chunkObject;
    
    public ChunkCoord coord;
    private bool _isActive;
    private bool activeOnInit;
    public bool isVoxelMapPopulated = false;
    public Vector3 position;
    bool isChunkLocked = false;
    Material[] materials = new Material[2];


    public byte[,,] voxelMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];
    public byte[,] heightMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkWidth];
    Subchunk[] subchunks = new Subchunk[VoxelData.ChunkSubdivisions];

    Queue<Queue<VoxelModification>> voxelModifications = new Queue<Queue<VoxelModification>>();
    SpawnableStructure spawnableStructures;
    bool SpawnableStructuresGenerated = false;
    World world;
    WorldGeneration worldGen;

    public Chunk(World _world, ChunkCoord _coord, bool generateOnLoad, bool _activeOnInit) {
        world = _world;
        worldGen = world.worldGen;
        coord = _coord;
        _isActive = true;
        spawnableStructures = new SS_Tree(world);
        activeOnInit = _activeOnInit;
        if (generateOnLoad)
            Init();
    }

    public void Init()
    {
        chunkObject = new GameObject();
        materials[0] = world.material;
        materials[1] = world.transparentMaterial;
        

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk " + coord.x + " " + coord.z;
        position = chunkObject.transform.position;

        for(byte subChunkIndex = 0; subChunkIndex < VoxelData.ChunkSubdivisions; subChunkIndex++) {
            subchunks[subChunkIndex] = new Subchunk(this, world, subChunkIndex, materials);
        }

        isChunkLocked = true;
        new Thread(new ThreadStart(PopulateVoxelMap)).Start();
        if (!activeOnInit)
            isActive = false;


    }
    void PopulateVoxelMap()
    {
        //Performs basic terrian generation
        for (int y = 0; y < VoxelData.ChunkHeight; y++) {
            for (int x = 0; x < VoxelData.ChunkWidth; x++) {
                for (int z = 0; z < VoxelData.ChunkWidth; z++) {
                    voxelMap[x, y, z] = worldGen.GenerateVoxel(new Vector3(x, y, z) + position).blockTypeIndex;
                }
            }
        }
        UpdateHeightMap();
        //Adds chunk to chunks in need of spawnable structures
        worldGen.GenerateSpawnableStructures(coord);
        _updateChunk();
        for (byte subChunkIndex = 0; subChunkIndex < VoxelData.ChunkSubdivisions; subChunkIndex++){
            subchunks[subChunkIndex].Init();
        }
        lock (world.subchunksToDraw)
        {
            for (byte subChunkIndex = 0; subChunkIndex < VoxelData.ChunkSubdivisions; subChunkIndex++){
                world.subchunksToDraw.Enqueue(subchunks[subChunkIndex].coord);
            }
        }
        isVoxelMapPopulated = true;
    }

    public void UpdateHeightMap(){
        for (int x = 0; x < VoxelData.ChunkWidth; x++){
            for (int z = 0; z < VoxelData.ChunkWidth; z++){
                for (int y = VoxelData.ChunkHeight-1; y >= 0; y--) { 
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid) {
                        heightMap[x, z] = (byte) y;
                        break;
                    }
                }
            }
        }
    }

    //Is called by World when neighbour chunks are populated. Will now spawn structures
    public void PopulateSpawnableStructures() {
        /*for (int i = 0; i < spawnableStructures.Length; i++){
            spawnableStructures[i].Populate(coord);
        }*/
        if (!SpawnableStructuresGenerated)
        {
            SpawnableStructuresGenerated = true;
            spawnableStructures.PopulateTarget(coord);
            new Thread(new ThreadStart(spawnableStructures.Populate)).Start();
        }
    }

    

    //Use global coords
    public BlockType GetBlockType(Vector3 globalPos) { 
        if (IsVoxelInChunk(globalPos)) {
            int[] coordinates = GetVoxelLocalCoordsFromGlobalVector3(globalPos);
            return BlockTypes.ALL_BLOCKS[voxelMap[coordinates[0], coordinates[1], coordinates[2]]];
        } else {
            return world.GetBlockType(globalPos);
        }
    }

    //Use local coords
    public byte GetBlockType(int localX, int localY, int localZ)
    {
        if (IsVoxelInChunk(localX, localY, localZ))
            return voxelMap[localX, localY, localZ];
        else
            return 0;
    }

    //Updates the voxel and subchunk on same thread.
    public void EditVoxel(Vector3 pos, BlockType newBlockType){
        int[] localCoords = GetVoxelLocalCoordsFromGlobalVector3(pos);
        VoxelModification voxMod = new VoxelModification(localCoords[0], localCoords[1], localCoords[2], newBlockType);
        Queue<VoxelModification> wrappedVoxMod = new Queue<VoxelModification>();
        wrappedVoxMod.Enqueue(voxMod);
        lock (voxelModifications){
            voxelModifications.Enqueue(wrappedVoxMod);
        }
        _updateChunk();
    }

    public void EditVoxel(int x, int y, int z, BlockType newBlockType)
    {
        VoxelModification voxMod = new VoxelModification(x, y, z, newBlockType);
        Queue<VoxelModification> wrappedVoxMod = new Queue<VoxelModification>();
        wrappedVoxMod.Enqueue(voxMod);
        lock (voxelModifications){
            voxelModifications.Enqueue(wrappedVoxMod);
        }
        //UpdateSurroundingVoxels(x, z);
    }

    public void EditMultipleBlocks(Queue<VoxelModification> voxMods) {
        lock (voxelModifications){
            voxelModifications.Enqueue(voxMods);
        }
    }

    public void UpdateSurroundingSubchunks(int x, int y, int z) {
        foreach (SubchunkCoord sc in GetAdjacentSubchunks(x, y, z))
            world.GetSubchunk(sc).UpdateSubChunk();
    }

    public List<SubchunkCoord> GetAdjacentSubchunks(int x, int y, int z) {
        List<SubchunkCoord> adjacentSubchunks = new List<SubchunkCoord>();
        byte subchunkIndex = Subchunk.GetSubchunkIndex(y);
        if (x == 0) {
            ChunkCoord adjacentChunk = new ChunkCoord(coord);
            adjacentChunk.x -= 1;
            if (world.isChunkInWorld(adjacentChunk) && world.GetChunk(adjacentChunk) != null)
                adjacentSubchunks.Add(world.GetChunk(adjacentChunk).GetSubchunk(subchunkIndex).coord);
        }
        else if(x == VoxelData.ChunkWidth - 1){
            ChunkCoord adjacentChunk = new ChunkCoord(coord);
            adjacentChunk.x += 1;
            if (world.isChunkInWorld(adjacentChunk) && world.GetChunk(adjacentChunk) != null)
                adjacentSubchunks.Add(world.GetChunk(adjacentChunk).GetSubchunk(subchunkIndex).coord);
        }
        if (y % VoxelData.ChunkSubdivisionHeight == 0 && y != 0) {
            adjacentSubchunks.Add(new SubchunkCoord(coord, (byte)(subchunkIndex - 1)));
        }
        else if (y % VoxelData.ChunkSubdivisionHeight == (VoxelData.ChunkSubdivisionHeight - 1) && y < VoxelData.ChunkHeight) {
            adjacentSubchunks.Add(new SubchunkCoord(coord, (byte)(subchunkIndex + 1)));
        }
        if (z == 0){
            ChunkCoord adjacentChunk = new ChunkCoord(coord);
            adjacentChunk.z -= 1;
            if (world.isChunkInWorld(adjacentChunk) && world.GetChunk(adjacentChunk) != null)
                adjacentSubchunks.Add(world.GetChunk(adjacentChunk).GetSubchunk(subchunkIndex).coord);
        }
        else if(z == VoxelData.ChunkWidth - 1){
            ChunkCoord adjacentChunk = new ChunkCoord(coord);
            adjacentChunk.z += 1;
            if (world.isChunkInWorld(adjacentChunk) && world.GetChunk(adjacentChunk) != null)
            {
                if (world.GetChunk(adjacentChunk).GetSubchunk(subchunkIndex) == null)
                    Debug.Log(world.GetChunk(adjacentChunk).GetSubchunk(subchunkIndex));
                adjacentSubchunks.Add(world.GetChunk(adjacentChunk).GetSubchunk(subchunkIndex).coord);
            }
        }
        return adjacentSubchunks;
    }

    public bool CheckVoxel(Vector3 pos) {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);
        if (!IsVoxelInChunk(x, y, z))
            return world.CheckForTransparentVoxel(pos + position);

        return world.blockTypes[voxelMap[x, y, z]].isTransparent;
    }

    public void UpdateChunk() {
        new Thread(new ThreadStart(_updateChunk)).Start();
    }

    public void _updateChunk(){
        isChunkLocked = true;
        bool[] subchunksToUpdate = new bool[VoxelData.ChunkSubdivisions];
        List<SubchunkCoord> adjacentSubchunksToUpdate = new List<SubchunkCoord>();
        lock (voxelModifications) {
            while (voxelModifications.Count > 0) {
                Queue<VoxelModification> voxModElement = voxelModifications.Dequeue();
                while (voxModElement.Count > 0) {
                    VoxelModification voxMod = voxModElement.Dequeue();
                    voxelMap[voxMod.x, voxMod.y, voxMod.z] = voxMod.newBlockType.blockTypeIndex;
                    subchunksToUpdate[Subchunk.GetSubchunkIndex(voxMod.y)] = true;
                    List <SubchunkCoord> adjacentSubchunks = GetAdjacentSubchunks(voxMod.x, voxMod.y, voxMod.z); 
                    foreach (SubchunkCoord adjacentSubchunk in adjacentSubchunks) {
                        if (adjacentSubchunk.superChunkCoord.Equals(coord))
                            subchunksToUpdate[adjacentSubchunk.subchunkIndex] = true;
                        else
                            adjacentSubchunksToUpdate.Add(adjacentSubchunk);
                    }
                }
            }
        }
        List<SubchunkCoord> updatedAdjacentSubchunks = new List<SubchunkCoord>();
        for (int i = 0; i < subchunksToUpdate.Length; i++) {
            if (subchunksToUpdate[i]) {
                subchunks[i].UpdateSubChunk();
            }
        }
        for (int i = 0; i < adjacentSubchunksToUpdate.Count; i++) {
            if (!updatedAdjacentSubchunks.Contains(adjacentSubchunksToUpdate[i])) {
                world.GetSubchunk(adjacentSubchunksToUpdate[i]).UpdateSubChunk();
                updatedAdjacentSubchunks.Add(adjacentSubchunksToUpdate[i]);
            }
        }
        isChunkLocked = false;
    }

    public bool IsVoxelInChunk(int x, int y, int z) {
        return !(x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1);
    }

    public bool IsVoxelInChunk(Vector3 globalPos){
        int[] coordinates = GetVoxelLocalCoordsFromGlobalVector3(globalPos);
        return !(coordinates[0] < 0 || coordinates[0] > VoxelData.ChunkWidth - 1 ||
                    coordinates[1] < 0 || coordinates[1] > VoxelData.ChunkHeight - 1 ||
                    coordinates[2] < 0 || coordinates[2] > VoxelData.ChunkWidth - 1);
    }

    public BlockType GetVoxelFromGlobalVector3(Vector3 pos){
        int[] localCoords = GetVoxelLocalCoordsFromGlobalVector3(pos);
        return BlockTypes.ALL_BLOCKS[voxelMap[localCoords[0], localCoords[1], localCoords[2]]];
    }

    public BlockType GetVoxelFromLocalCoords(int x, int y, int z) {
        return BlockTypes.ALL_BLOCKS[voxelMap[x, y, z]];
    }

    public int[] GetVoxelLocalCoordsFromGlobalVector3(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);
        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);
        return new int[] { xCheck, yCheck, zCheck};
    }

    public Subchunk GetSubchunk(int index) {
        return subchunks[index];
    }

    public bool isActive {
        get { return _isActive; }
        set { _isActive = value;
            if(chunkObject != null)
                chunkObject.SetActive(value); }
    }

    public override string ToString() {
        return "Chunk X:" + coord.x + " Z:" + coord.z;
    }
}

public class ChunkCoord {

    public int x,z;

    public ChunkCoord(int x, int z) {
        this.x = x;
        this.z = z;
    }

    public ChunkCoord(ChunkCoord cc)
    {
        this.x = cc.x;
        this.z = cc.z;
    }

    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }

    public ChunkCoord(Vector3 positionInVoxel)
    {
        x = Mathf.FloorToInt(positionInVoxel.x / VoxelData.ChunkWidth);
        z = Mathf.FloorToInt(positionInVoxel.z / VoxelData.ChunkWidth);
    }


    public bool Equals(ChunkCoord other) {
        if (other == null) return false;
        else if (other.x == x && other.z == z) return true;
        else return false;
    }
}

public class VoxelModification {

    public int x, y, z;
    public BlockType newBlockType;

    public VoxelModification(int x, int y, int z, BlockType newBlockType) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.newBlockType = newBlockType;
    }

    public override string ToString() {
        return "VoxMod " + x + " " + y + " " + z + " newBlock: " + newBlockType.blockName;
    }
}
