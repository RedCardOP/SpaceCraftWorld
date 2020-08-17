using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "TerrainGen/Biome Attributes")]
public class BiomeAttributes : ScriptableObject
{
    public string biomeName;
    public int solidGroundHeight, terrainHeight;
    public float terrainScale;
    public Lode[] lodes;
    public SpawnableStructure[] spawnableStructures;
}

[System.Serializable]
public class Lode { 
    public string nodeName;
    public byte blockID;
    public int minHeight, maxHeight;
    public float scale, threshold, noiseOffset;


}