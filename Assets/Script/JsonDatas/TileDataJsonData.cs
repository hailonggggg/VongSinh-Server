using System;
using UnityEngine;

[Serializable]
public class TileDataJsonData
{
    public Vector3Int GridPosition;
    public bool IsWalkable;
    public bool IsSpawnPoint;
    public bool IsOpponentSpawnPoint;
}
