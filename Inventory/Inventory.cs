using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Inventory{

    Item GetItem(short index);
    short GetInventoryVolume();
    short GetMaxInventoryVolume();
    short GetAllocatedSlots();

}
