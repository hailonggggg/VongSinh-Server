using System;
using UnityEngine;

[Serializable]
public class TileData
{
    public Vector3Int GridPosition;
    public bool IsWalkable;
    public bool IsSpawnPoint;
    public bool IsOpponentSpawnPoint;
}