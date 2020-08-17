using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceSettings {

    public static SpawnableStructureBuffer spawnableStructureBufferSetting = SpawnableStructureBuffer.SINGLE;

}
public enum SpawnableStructureBuffer { NONE = 0, SINGLE = 1, DOUBLE = 2 };