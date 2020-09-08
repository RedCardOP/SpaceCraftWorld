using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDrop : Entity{

    GameObject itemDropGameObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    World world;

    Item item;
    public Vector3 velocity;

    public ItemDrop(Item item, Vector3 initialVelocity, Vector3 initalPosition, World world, Material[] materials) {
        itemDropGameObject = new GameObject();
        this.item = item;
        this.world = world;
        meshFilter = itemDropGameObject.AddComponent<MeshFilter>();
        meshRenderer = itemDropGameObject.AddComponent<MeshRenderer>();
        meshRenderer.materials = materials;
        itemDropGameObject.transform.position = initalPosition;
        this.velocity = initialVelocity;
        CreateMesh();
        EntityManager.entities.Add(this);
        float scaleFactor = 0.20f + item.itemVolume * 0.001f;
        itemDropGameObject.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
    }

    public override void FixedUpdate() {
        MoveEntity(velocity, world);
    }

    void CreateMesh() {
        int vertexIndex = 0;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<int> transparentTriangles = new List<int>();
        Material[] materials = new Material[2];
        List<Vector2> uvs = new List<Vector2>();

        BlockType blockType = item.GetItemType().GetBlockType();
        bool isTransparent = blockType.isTransparent;
        for (int p = 0; p < 6; p++) {
            vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
            vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
            vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
            vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

            AddTexture(blockType.GetTextureID(p), uvs);

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
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);

        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    void AddTexture(int textureId, List<Vector2> uvs) {
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

    public override void Interact(Entity entity) {
        if (entity is Player p)
            p.PickupItem(this);
    }

    public override BoundingBox GetEntityBoundingBox() {
        BoundingBox itemDropBoundingBox = new BoundingBox(itemDropGameObject.transform.position, itemDropGameObject.transform.localScale);
        itemDropBoundingBox.offsetCenter();
        return itemDropBoundingBox;
    }

    protected override void TranslateEntity(Vector3 velocity) {
        itemDropGameObject.transform.Translate(velocity, Space.World);
    }

    public Item GetItem() {
        return item;
    }

    public void Destroy() {
        EntityManager.entities.Remove(this);
        UnityEngine.Object.Destroy(itemDropGameObject);
    }
}
