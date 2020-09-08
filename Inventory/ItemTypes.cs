using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class ItemTypes {
    public static readonly ItemType NO_ITEM = new ItemType("", null);                   //Index 0
    public static readonly ItemType DIRT = new ItemType("Dirt", BlockTypes.DIRT);       //Index 1
    public static readonly ItemType GRASS = new ItemType("Grass", BlockTypes.GRASS);     //Index 2
    public static readonly ItemType STONE = new ItemType("Stone", BlockTypes.STONE);    //Index 3
    public static readonly ItemType WOOD = new ItemType("Wood", BlockTypes.WOOD);       //Index 4
    public static readonly ItemType IRON_ORE = new ItemType("Iron Ore", BlockTypes.IRON_ORE); //Index 5
    public static readonly ItemType[] ALL_ITEMS = { NO_ITEM, DIRT, GRASS, STONE, WOOD, IRON_ORE };


}

public class ItemType{

    string itemName;
    BlockType blockType;

    public ItemType(string itemName, BlockType blockType) {
        this.itemName = itemName;
        this.blockType = blockType;
    }

    public string GetItemName(){
        return itemName;
    }

    public BlockType GetBlockType() {
        return blockType;
    }


}
