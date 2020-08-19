using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Chunk
{
    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    public ChunkCoord coord;
    private bool _isActive;
    private bool activeOnInit;
    public bool isVoxelMapPopulated = false;
    public Vector3 position;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();

    public byte[,,] voxelMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];
    public byte[,] heightMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkWidth];
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
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk " + coord.x + " " + coord.z;
        position = chunkObject.transform.position;

        PopulateVoxelMap();
        UpdateChunk();
        if (!activeOnInit)
            isActive = false;

    }
    void PopulateVoxelMap()
    {
        //Performs basic terrian generation
        for (int y = 0; y < VoxelData.ChunkHeight; y++) {
            for (int x = 0; x < VoxelData.ChunkWidth; x++) {
                for (int z = 0; z < VoxelData.ChunkWidth; z++) {
                    voxelMap[x, y, z] = worldGen.GenerateVoxel(new Vector3(x, y, z) + position);
                }
            }
        }
        UpdateHeightMap();
        isVoxelMapPopulated = true;
        //Adds chunk to chunks in need of spawnable structures
        worldGen.GenerateSpawnableStructures(coord);
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
            spawnableStructures.Populate(coord);
            SpawnableStructuresGenerated = true;
        }
    }

    void UpdateMeshData(Vector3 pos){
        byte blockID = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];
        bool isTransparent = world.blockTypes[blockID].isTransparent;
        for (int p = 0; p < 6; p++) {
            if (CheckVoxel(pos + VoxelData.faceChecks[p])) {
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]] + pos);
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]] + pos);
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]] + pos);
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]] + pos);

                AddTexture(world.blockTypes[blockID].GetTextureID(p));

                if (!isTransparent){
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                    vertexIndex += 4;
                } else {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                    vertexIndex += 4;
                }
              
            }
        }
    }

    //Use global coords
    public byte GetBlockType(Vector3 globalPos) { 
        if (IsVoxelInChunk(globalPos)) {
            int[] coordinates = GetVoxelLocalCoordsFromGlobalVector3(globalPos);
            return voxelMap[coordinates[0], coordinates[1], coordinates[2]];
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

    public void EditVoxel(Vector3 pos, byte newBlockID){
        int[] localCoords = GetVoxelLocalCoordsFromGlobalVector3(pos);
        voxelMap[localCoords[0], localCoords[1], localCoords[2]] = newBlockID;

        UpdateChunk();
        UpdateSurroundingVoxels(localCoords[0], localCoords[2]);
    }

    public void EditVoxel(int x, int y, int z, byte newBlockID)
    {
        voxelMap[x, y, z] = newBlockID;

        UpdateChunk();
        UpdateSurroundingVoxels(x, z);
    }

    public void EditVoxelWithoutMeshUpdate(int x, int y, int z, byte newBlockID)
    {
        if (IsVoxelInChunk(x,y,z))
            voxelMap[x, y, z] = newBlockID;
    }

    public void UpdateSurroundingVoxels(int x, int z) {
        ChunkCoord adjacentChunk = new ChunkCoord(coord);
        if (x == 0) {
            adjacentChunk.x--;
            if (world.isChunkInWorld(adjacentChunk) && world.GetChunk(adjacentChunk) != null)
                world.GetChunk(adjacentChunk).UpdateChunk();
        }
        else if(x == VoxelData.ChunkWidth - 1){
            adjacentChunk.x++;
            if (world.isChunkInWorld(adjacentChunk) && world.GetChunk(adjacentChunk) != null)
                world.GetChunk(adjacentChunk).UpdateChunk();
        }
        if(z == 0){
            adjacentChunk.z--;
            if (world.isChunkInWorld(adjacentChunk) && world.GetChunk(adjacentChunk) != null)
                world.GetChunk(adjacentChunk).UpdateChunk();
        }
        else if(z == VoxelData.ChunkWidth - 1){
            adjacentChunk.z++;
            if (world.isChunkInWorld(adjacentChunk) && world.GetChunk(adjacentChunk) != null)
                world.GetChunk(adjacentChunk).UpdateChunk();
        }
    }

    bool CheckVoxel(Vector3 pos) {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);
        if (!IsVoxelInChunk(x, y, z))
            return world.CheckForTransparentVoxel(pos + position);

        return world.blockTypes[voxelMap[x, y, z]].isTransparent;
    }

    void CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);

        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    public void UpdateChunk() {
        ClearMeshData();
        for (int y = 0; y < VoxelData.ChunkHeight; y++) { 
            for (int x = 0; x < VoxelData.ChunkWidth; x++) {
                for (int z = 0; z < VoxelData.ChunkWidth; z++) { 
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                        UpdateMeshData(new Vector3(x, y, z));
                }
            }
        }
        CreateMesh();
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
    }
    void AddTexture(int textureId) {
        float y = textureId / VoxelData.TexturePackSizeInBlocks;
        float x = textureId % VoxelData.TexturePackSizeInBlocks;

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;
        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
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

    public byte GetVoxelFromGlobalVector3(Vector3 pos){
        int[] localCoords = GetVoxelLocalCoordsFromGlobalVector3(pos);
        return voxelMap[localCoords[0], localCoords[1], localCoords[2]];
    }

    public int[] GetVoxelLocalCoordsFromGlobalVector3(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);
        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);
        return new int[] { xCheck, yCheck, zCheck};
    }

    public bool isActive {
        get { return _isActive; }
        set { _isActive = value;
            if(chunkObject != null)
                chunkObject.SetActive(value); }
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
