using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Map
{
    public GridFacing LeftSpawnFacing;
    public GridFacing RightSpawnFacing;
    public List<TileData> Tiles = new();

    private HashSet<Vector3Int> leftSpawnPoint = new();
    private HashSet<Vector3Int> rightSpawnPoint = new();

    public void LoadData(MapDataJsonData mapDataJsonData)
    {
        LeftSpawnFacing = Enum.Parse<GridFacing>(mapDataJsonData.PlayerSpawnFacing);
        RightSpawnFacing = Enum.Parse<GridFacing>(mapDataJsonData.EnemySpawnFacing);
        foreach (var tileJson in mapDataJsonData.Tiles)
        {
            TileData tile = new()
            {
                GridPosition = tileJson.GridPosition,
                IsWalkable = tileJson.IsWalkable,
                IsSpawnPoint = tileJson.IsSpawnPoint,
                IsOpponentSpawnPoint = tileJson.IsOpponentSpawnPoint
            };
            Tiles.Add(tile);
            if (tile.IsSpawnPoint) leftSpawnPoint.Add(tile.GridPosition);
            else if (tile.IsOpponentSpawnPoint) rightSpawnPoint.Add(tile.GridPosition);
        }
    }

    public bool IsValidSpawnPointPosition(bool isLeftSide, UnityEngine.Vector3Int placedPosition)
    {
        return isLeftSide ? leftSpawnPoint.Contains(placedPosition) : rightSpawnPoint.Contains(placedPosition);
    }
}

public enum GridFacing
{
    Up,
    Down,
    Left,
    Right
}