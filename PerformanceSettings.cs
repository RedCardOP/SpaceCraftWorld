using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceSettings {

    public static SpawnableStructureBuffer spawnableStructureBufferSetting = SpawnableStructureBuffer.SINGLE;
    public static readonly int chunksToDrawPerFrame = 2;
    public static int subchunksToDrawPerFrame { get { return chunksToDrawPerFrame * VoxelData.ChunkSubdivisions; }}

}
public enum SpawnableStructureBuffer { NONE = 0, SINGLE = 1, DOUBLE = 2 };