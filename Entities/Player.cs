using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;

public class Player : Entity
{
    private GameObject playerGameObject;
    private Transform camera;
    private World world;
    public Transform hightlightBlock, placeBlock;
    public MeshFilter placeBlockMesh;
    
    public Text debugOverlay;
    public bool debugOverlayActive = true;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 6f;
    public float playerWidth = 0.15f;
    public bool isGrounded, isSprinting;
    public float sensitivityMultiplier = 1f;

    public float checkIncrement = 0.1f;
    public float reach = 8f;

    private float horizontal, vertical, mouseHorizontal, mouseVertical, verticalMomentum;
    public Vector3 velocity;
    public bool jumpRequest;

    public byte selectedBlockIndex = 1;
    public StorageInventory playerInventory;
    public Entity highlightedEntity;

    public Player(World world, Transform hightlightBlock, Transform placeBlock, MeshFilter placeBlockMesh, Text debugOverlay) {
        EntityManager.entities.Add(this);
        EntityManager.entitiesToFrameUpdate.Add(this);
        this.world = world;
        this.hightlightBlock = hightlightBlock;
        this.placeBlock = placeBlock;
        this.placeBlockMesh = placeBlockMesh;
        this.debugOverlay = debugOverlay;
    }

    public override void Start() {
        camera = GameObject.Find("Main Camera").transform;
        playerGameObject = GameObject.Find("Player");
        world = GameObject.Find("World").GetComponent<World>();
        Cursor.lockState = CursorLockMode.Locked;
        playerInventory = new StorageInventory(100);
    }

    public override void Update() {
        GetPlayerInputs();
        placeCursorBlocks();
        UpdateDebugOverlay();
    }

    public override void FixedUpdate() {
        if (jumpRequest)
            Jump();
        CalculateVelocity();
        Vector3 finalVelocity = MoveEntity(velocity, world);
        if (finalVelocity.y == 0)
            isGrounded = true;
        else
            isGrounded = false;

        playerGameObject.transform.Rotate(Vector3.up * mouseHorizontal * sensitivityMultiplier);
        camera.Rotate(Vector3.right * -mouseVertical * sensitivityMultiplier);

        if (world.dropItem) {
            world.dropItem = false;
            Material[] mats = { world.material, world.transparentMaterial };
            new ItemDrop(new Item(ItemTypes.STONE, 10), new Vector3(0, -0.25f, 0), playerGameObject.transform.position, world, mats);
        }
    }

    private void UpdateDebugOverlay() {
        if(Input.GetButtonDown("ToggleDebugOverlay")) {
            debugOverlayActive = !debugOverlayActive;
            debugOverlay.gameObject.SetActive(debugOverlayActive);
        }
        if (debugOverlayActive) {
            Vector3 pos = World.GetFlooredVector3(playerGameObject.transform.position);
            debugOverlay.text = "Block Position: X: " + (int)pos.x + " Y: " + (int)pos.y + " Z: " + (int)pos.z +
                                "\nChunk X: " + world.playerLastChunkCoord.x + " Y: " + Subchunk.GetSubchunkIndex((int)pos.y) + " Z: " + world.playerLastChunkCoord.z +
                                "\nFPS: " + Mathf.FloorToInt(1f/Time.deltaTime);
            if (Mathf.FloorToInt(1f / Time.deltaTime) < 30)
                Debug.Log("FPS Hit: " + Mathf.FloorToInt(1f / Time.deltaTime));
        }
    }

    private void CalculateVelocity() {
        if (verticalMomentum > world.gravity)
            verticalMomentum += Time.fixedDeltaTime * world.gravity;
        //Horizontal movement
        if (isSprinting)
            velocity = ((playerGameObject.transform.forward * vertical) + (playerGameObject.transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        else
            velocity = ((playerGameObject.transform.forward * vertical) + (playerGameObject.transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;
        //Vertical movement
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;
        
    }

    private void Jump() {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void GetPlayerInputs() {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");
        if (Input.GetButtonDown("Sprint"))
            isSprinting = true;
        if (Input.GetButtonUp("Sprint"))
            isSprinting = false;
        if (isGrounded && Input.GetButton("Jump"))
            jumpRequest = true;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0) {
            if (scroll > 0)
                selectedBlockIndex++;
            else
                selectedBlockIndex--;
            //Allows for scrolling back around to the beginning
            if (selectedBlockIndex > (byte)(BlockTypes.ALL_BLOCKS.Length - 1))
                selectedBlockIndex = 1;
            if (selectedBlockIndex < 1)
                selectedBlockIndex = (byte)(BlockTypes.ALL_BLOCKS.Length - 1);
            updatePlaceBlockType();
        }

        if (hightlightBlock.gameObject.activeSelf) {
            //Destroy Block
            if (Input.GetMouseButtonDown(0)) {
                Chunk c = world.GetChunk(hightlightBlock.position);
                Item blockItem = new Item(c.GetBlockType(hightlightBlock.position).GetItemType(), 1);
                if(blockItem.GetItemType() != ItemTypes.NO_ITEM)
                    playerInventory.AddItem(blockItem);
                c.EditVoxel(hightlightBlock.position, BlockTypes.AIR);
            }
            //Place Block
            else if (Input.GetMouseButtonDown(1)) {
                world.GetChunk(placeBlock.position).EditVoxel(placeBlock.position, BlockTypes.ALL_BLOCKS[selectedBlockIndex]);
            }
        }

        if(highlightedEntity != null) {
            if (Input.GetButtonDown("EntityInteract"))
                highlightedEntity.Interact(this);
        }
    }

    private void placeCursorBlocks() {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();
        BoundingBox pointerCheckBoundingBox = new BoundingBox(lastPos, 0.1f, 0.1f, 0.1f);
        while (step < reach) {
            Vector3 pos = camera.position + (camera.forward * step);
            pointerCheckBoundingBox.UpdateLocation(pos);
            Entity entityIntersecting = EntityManager.EntityIntersecting(pointerCheckBoundingBox, typeof(ItemDrop));
            if (world.CheckForVoxel(pos)) {
                hightlightBlock.position = World.GetFlooredVector3(pos);
                placeBlock.position = World.GetFlooredVector3(lastPos);
                hightlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);
                updatePlaceBlockType();
                return;
            } else if(entityIntersecting != null) { 
                if (entityIntersecting.GetType().Equals(typeof(ItemDrop))) {
                    highlightedEntity = entityIntersecting;
                    return;
                }
            }
            lastPos = World.GetFlooredVector3(pos);
            step += checkIncrement;
        }

        hightlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);
        highlightedEntity = null;
    }

    private void updatePlaceBlockType(){

        int vertexIndex = 0;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int p = 0; p < 6; p++)
        {
            vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
            vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
            vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
            vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

            float y = BlockTypes.ALL_BLOCKS[selectedBlockIndex].GetTextureID(p) / VoxelData.TexturePackSizeInBlocks;
            float x = BlockTypes.ALL_BLOCKS[selectedBlockIndex].GetTextureID(p) % VoxelData.TexturePackSizeInBlocks;

            x *= VoxelData.NormalizedBlockTextureSize;
            y *= VoxelData.NormalizedBlockTextureSize;

            y = 1f - y - VoxelData.NormalizedBlockTextureSize;
            uvs.Add(new Vector2(x, y));
            uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
            uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
            uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 3);
            vertexIndex += 4;
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        placeBlockMesh.mesh = mesh;
    }

    public void PickupItem(ItemDrop itemDrop) {
        playerInventory.AddItem(itemDrop.GetItem());
        itemDrop.Destroy();
    }

    public override BoundingBox GetEntityBoundingBox() {
        BoundingBox playerBoundingBox = new BoundingBox(playerGameObject.transform.position, 0.7f, 2f, 0.7f);
        playerBoundingBox.offsetCenterYOnly();
        return playerBoundingBox;
    }

    protected override void TranslateEntity(Vector3 velocity) {
        playerGameObject.transform.Translate(velocity, Space.World);
    }

    public override void Interact(Entity entity) {
        throw new System.NotImplementedException();
    }

    public Vector3 position { 
        get { return playerGameObject.transform.position; }
        set { playerGameObject.transform.position = value; }
    }

}
