using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorageInventory{

    public static readonly byte SLOTS_PER_LINE = 5;
    short maxInvVolume;
    short inventoryVolume;
    ushort allocatedSlots;
    List<Item> items;

    public StorageInventory(short maxInvVolume)
    {
        this.maxInvVolume = maxInvVolume;
        allocatedSlots = 0;
        items = new List<Item>();
    }

    public short GetMaxInventoryVolume() {
        return maxInvVolume;
    }
    public ushort GetAllocatedSlots() {
        return allocatedSlots;
    }
    public short GetInventoryVolume() {
        return inventoryVolume;
    }

    //Returns clone of item at given inventory index
    public Item GetItem(short index){
        if (items.Count - 1 < index)
            return null;
        return items[index].Clone();
    }

    //Returns NON-clone array of all items
    public Item[] GetItems()
    {
        return items.ToArray();
    }

    public bool AddItem(Item item) { 
        foreach(Item i in items) {
            if (i.GetItemType().Equals(item.GetItemType())) { 
                if (item.itemVolume + GetInventoryVolume() <= GetMaxInventoryVolume()) {
                    i.itemVolume += item.itemVolume;
                    RecalculatedInventoryFields();
                    return true;
                } 
                else return false;
            }
        }
        return AddItemOnNewStack(item);
    }
    
    public bool AddItemOnNewStack(Item item) { 
        if (item.itemVolume + GetInventoryVolume() <= GetMaxInventoryVolume()){
            items.Add(item);
            RecalculatedInventoryFields();
            return true;
        }
        else return false;
    }

    public Item RemoveItem(short index) {
        if (items.Count <= index) 
            return null;
        Item item = items[index];
        items.RemoveAt(index);
        return item;
    }

    public Item RemoveItem(short index, short volumeToRemove) {
        if (items.Count <= index) 
            return null;
        Item item = items[index];
        if (item.itemVolume < volumeToRemove)
            return null;
        else if (item.itemVolume == volumeToRemove)
            return RemoveItem(index);
        Item itemToReturn = item.Clone();
        itemToReturn.itemVolume = volumeToRemove;
        item.itemVolume -= volumeToRemove;
        return itemToReturn;
    }


    void RecalculatedInventoryFields() {
        RecalclateAllocatedSlots();
        RecalculateVolume();
    }

    void RecalculateVolume() {
        short totalVolume = 0;
        foreach (Item i in items) {
            totalVolume += i.itemVolume;
            Debug.Log("Item: " + i.GetItemType().GetItemName() + " vol: " + i.itemVolume);
        }
        inventoryVolume = totalVolume;
        Debug.Log("Inv Vol: " + inventoryVolume + " AlloSlots: " + allocatedSlots);
    }

    void RecalclateAllocatedSlots() { 
        allocatedSlots = (ushort)((items.Count % SLOTS_PER_LINE + 1) * SLOTS_PER_LINE);
    }

}
