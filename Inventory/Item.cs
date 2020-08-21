using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Item {

    ItemType itemType;
    public short itemVolume;

    public Item(ItemType itemType, short itemVolume)
    {
        this.itemType = itemType;
        this.itemVolume = itemVolume;
    }

    public ItemType GetItemType() { return itemType; }

    //Returns clone of current item
    public Item Clone() {
        return new Item(itemType, itemVolume);
    }

}