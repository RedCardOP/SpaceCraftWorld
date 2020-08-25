using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Subchunk {
    
    Chunk superChunk;
    World world;
    public SubchunkCoord coord;
    GameObject subchunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    object meshLock = new object();
    bool isSubchunkLocked = false;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();

    public Vector3 position;
    byte subchunkIndex;

    public Subchunk(Chunk superChunk, World world, byte subchunkIndex, Material[] materials) {
        subchunkObject = new GameObject();
        this.superChunk = superChunk;
        this.world = world;
        meshFilter = subchunkObject.AddComponent<MeshFilter>();
        meshRenderer = subchunkObject.AddComponent<MeshRenderer>();

        this.materials = materials;
        meshRenderer.materials = materials;
        subchunkObject.transform.SetParent(superChunk.chunkObject.transform);
        subchunkObject.transform.position = new Vector3(superChunk.position.x, subchunkIndex * VoxelData.ChunkSubdivisionHeight, superChunk.position.z);
        subchunkObject.name = "Subchunk " + subchunkIndex;
        this.subchunkIndex = subchunkIndex;
        position = subchunkObject.transform.position;
        coord = new SubchunkCoord(superChunk.coord, subchunkIndex);
    }

    public void UpdateSubChunk() {
        lock (meshLock) {
            ClearMeshData();
            int startingY = subchunkIndex * VoxelData.ChunkSubdivisionHeight;
            for (int y = startingY; y < startingY + VoxelData.ChunkSubdivisionHeight; y++){
                for (int x = 0; x < VoxelData.ChunkWidth; x++){
                    for (int z = 0; z < VoxelData.ChunkWidth; z++){
                        if (BlockTypes.ALL_BLOCKS[superChunk.voxelMap[x, y, z]].isSolid)
                            UpdateMeshData(new Vector3(x, y, z));
                    }
                }
            }
        }
        lock (world.subchunksToDraw){
            world.subchunksToDraw.Enqueue(coord);
        }
    }

    void UpdateMeshData(Vector3 chunkLocalPos){
        BlockType blockType = BlockTypes.ALL_BLOCKS[superChunk.voxelMap[(int)chunkLocalPos.x, (int)chunkLocalPos.y, (int)chunkLocalPos.z]];
        bool isTransparent = blockType.isTransparent;
        Vector3 subchunkLocalizedPosition = chunkLocalPos;
        subchunkLocalizedPosition.y %= VoxelData.ChunkSubdivisionHeight;
        for (int p = 0; p < 6; p++) {
            if (superChunk.CheckVoxel(chunkLocalPos + VoxelData.faceChecks[p])) {
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]] + subchunkLocalizedPosition);
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]] + subchunkLocalizedPosition);
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]] + subchunkLocalizedPosition);
                vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]] + subchunkLocalizedPosition);

                AddTexture(blockType.GetTextureID(p));

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

    public void CreateMesh() {
        lock (meshLock)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();

            mesh.subMeshCount = 2;
            mesh.SetTriangles(triangles.ToArray(), 0);
            mesh.SetTriangles(transparentTriangles.ToArray(), 1);

            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
            subchunkObject.name = subchunkObject.name + " " + vertices.Count;
        }
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

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
    }

    public void Init() {
        UpdateSubChunk();
    }

    public static byte GetSubchunkIndex(int y) {
        return (byte)(y / VoxelData.ChunkSubdivisionHeight);
    }

    public bool isEditable {
        get { return (!isSubchunkLocked); }
    }

    public override string ToString(){
        return "Subchunk " + coord.superChunkCoord.x + " " + coord.subchunkIndex + " " + coord.superChunkCoord.z;
    }

}

public class SubchunkCoord {
    public ChunkCoord superChunkCoord;
    public byte subchunkIndex;

    public SubchunkCoord(ChunkCoord superChunkCoord, byte subchunkIndex) {
        this.superChunkCoord = superChunkCoord;
        this.subchunkIndex = subchunkIndex;
    }
}
