using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapDataJsonData
{
    public int Id;
    public string AssetName;
    public string PlayerSpawnFacing;
    public string EnemySpawnFacing;
    public List<TileDataJsonData> Tiles;
}
