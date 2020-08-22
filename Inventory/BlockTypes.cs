using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Xml;
using UnityEngine;

public class BlockTypes {

    
    public static readonly BlockType AIR = new BlockType("Air", false, true, 0, 0, 0, 0, 0, 0, 0, 0);          //Index 0
    public static readonly BlockType GRASS = new BlockType("Grass", true, false, 1, 1, 1, 1, 0, 1, 1, 1);      //Index 1
    public static readonly BlockType DIRT = new BlockType("Dirt", true, false, 1, 2, 1, 1, 1, 1, 1, 1);        //Index 2
    public static readonly BlockType STONE = new BlockType("Stone", true, false, 3, 3, 2, 2, 2, 2, 2, 2);      //Index 3
    public static readonly BlockType WOOD = new BlockType("Wood", true, false, 4, 4, 3, 3, 5, 5, 3, 3);        //Index 4
    public static readonly BlockType LEAVES = new BlockType("Leaves", false, true, 0, 5, 4, 4, 4, 4, 4, 4);    //Index 5
    public static readonly BlockType IRON_ORE = new BlockType("Iron Ore", true, false, 5, 6, 8, 8, 8, 8, 8, 8);//Index 6
    public static readonly BlockType EOTW = new BlockType("EOTW", true, false, 0, 7, 15, 15, 15, 15, 15, 15);  //Index 7
    
    public static readonly BlockType[] ALL_BLOCKS = { AIR, GRASS, DIRT, STONE, WOOD, LEAVES, IRON_ORE, EOTW };
}


[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;
    public bool isTransparent;
    //0 is non-existent item
    public ushort itemTypeIndex;
    public byte blockTypeIndex;

    [Header("Texture ID Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    public BlockType(string blockName, bool isSolid, bool isTransparent, ushort itemTypeIndex, byte blockTypeIndex, int backFace, int frontFace, int topFace, int bottomFace, int leftFace, int rightFace) {
        this.blockName = blockName;
        this.isSolid = isSolid;
        this.isTransparent = isTransparent;
        this.itemTypeIndex = itemTypeIndex;
        this.blockTypeIndex = blockTypeIndex;
        this.backFaceTexture = backFace;
        this.frontFaceTexture = frontFace;
        this.topFaceTexture = topFace;
        this.bottomFaceTexture = bottomFace;
        this.leftFaceTexture = leftFace;
        this.rightFaceTexture = rightFace;
    }

    public int GetTextureID(int faceIndex)
    {

        switch (faceIndex)
        {

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

    public ItemType GetItemType() {
        return ItemTypes.ALL_ITEMS[itemTypeIndex];
    }

}
