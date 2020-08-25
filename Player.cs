using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    private Transform camera;
    private World world;
    public Transform hightlightBlock, placeBlock;
    public MeshFilter placeBlockMesh;
    
    public Text debugOverlay;
    public bool debugOverlayActive;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float gravity = -9.8f;
    public float jumpForce = 5f;
    public float playerWidth = 0.15f;
    public bool isGrounded, isSprinting;
    public float sensitivityMultiplier = 1f;

    public float checkIncrement = 0.1f;
    public float reach = 8f;

    private float horizontal, vertical, mouseHorizontal, mouseVertical, verticalMomentum;
    private Vector3 velocity;
    private bool jumpRequest;

    public byte selectedBlockIndex = 1;
    public StorageInventory playerInventory;

    private void Start() {
        camera = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();
        Cursor.lockState = CursorLockMode.Locked;
        playerInventory = new StorageInventory(100);
    }

    private void Update() {
        GetPlayerInputs();
        placeCursorBlocks();
        UpdateDebugOverlay();
    }

    private void FixedUpdate() {
        CalculateVelocity();
        if (jumpRequest)
            Jump();

        transform.Rotate(Vector3.up * mouseHorizontal * sensitivityMultiplier);
        camera.Rotate(Vector3.right * -mouseVertical * sensitivityMultiplier);
        transform.Translate(velocity, Space.World);

    }

    private void UpdateDebugOverlay() {
        if(Input.GetButtonDown("Toggle Debug Overlay")) {
            debugOverlayActive = !debugOverlayActive;
            debugOverlay.gameObject.SetActive(debugOverlayActive);
        }
        if (debugOverlayActive) {
            Vector3 pos = world.GetFlooredVector3(transform.position);
            debugOverlay.text = "Block Position: X: " + (int)pos.x + " Y: " + (int)pos.y + " Z: " + (int)pos.z +
                                "\nChunk X: " + world.playerLastChunkCoord.x + " Y: " + Subchunk.GetSubchunkIndex((int)pos.y) + " Z: " + world.playerLastChunkCoord.z +
                                "\nFPS: " + Mathf.FloorToInt(1f/Time.deltaTime);
            if (Mathf.FloorToInt(1f / Time.deltaTime) < 15)
                Debug.LogWarning("FPS Hit: " + Mathf.FloorToInt(1f / Time.deltaTime));
        }
    }

    private void CalculateVelocity() {
        if (verticalMomentum > gravity)
            verticalMomentum += Time.fixedDeltaTime * gravity;
        //Horizontal movement
        if (isSprinting)
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        else
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;
        //Vertical movement
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;
        //Collision detection
        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
            velocity.z = 0;
        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
            velocity.x = 0;
        if (velocity.y < 0)
            velocity.y = checkDownSpeed(velocity.y);
        else if (velocity.y > 0)
            velocity.y = checkUpSpeed(velocity.y);

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
    }

    private void placeCursorBlocks() {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();
        while (step < reach) {
            Vector3 pos = camera.position + (camera.forward * step);
            if (world.CheckForVoxel(pos)) {
                hightlightBlock.position = world.GetFlooredVector3(pos);
                placeBlock.position = world.GetFlooredVector3(lastPos);
                hightlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);
                updatePlaceBlockType();
                return;
            }
            lastPos = world.GetFlooredVector3(pos);
            step += checkIncrement;
        }
        hightlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);
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

    private float checkDownSpeed(float downSpeed) {
        if(world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))){
            isGrounded = true;
            return 0;
        }
        isGrounded = false;
        return downSpeed;
    }

    private float checkUpSpeed(float upSpeed) {
        if(world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth))){
            return 0;
        }
        return upSpeed;
    }

    public bool front {
        get {
            return (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1, transform.position.z + playerWidth)));
        }
    }

    public bool back
    {
        get
        {
            return (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1, transform.position.z - playerWidth)));
        }
    }
    public bool left
    {
        get
        {
            return (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1, transform.position.z)));
        }
    }
    public bool right
    {
        get
        {
            return (world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1, transform.position.z + playerWidth)));
        }
    }


}
