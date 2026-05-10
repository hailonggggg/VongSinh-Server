using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Map
{
    public GridFacing LeftSpawnFacing;
    public GridFacing RightSpawnFacing;
    public List<TileData> LeftTiles = new();
    public List<TileData> rightTiles = new();

    public List<TileData> TileDatas => LeftTiles.Concat(rightTiles).ToList();
    private HashSet<Vector3Int> walkablePositions => TileDatas.Where(x => x.IsWalkable).Select(x => x.GridPosition).ToHashSet();

    private static readonly Vector3Int[] Directions =
 {
        Vector3Int.left ,
        Vector3Int.up,
        Vector3Int.right,
        Vector3Int.down
    };

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
                IsSpawnPoint = tileJson.IsSpawnPoint || tileJson.IsOpponentSpawnPoint,
            };
            if (tileJson.IsSpawnPoint)
                LeftTiles.Add(tile);
            else if (tileJson.IsOpponentSpawnPoint)
                rightTiles.Add(tile);
        }
    }

    public bool IsValidSpawnPointPosition(bool isLeftSide, UnityEngine.Vector3Int placedPosition)
    {
        return isLeftSide ? LeftTiles.Select(x => x.GridPosition).Contains(placedPosition)
        : rightTiles.Select(x => x.GridPosition).Contains(placedPosition);
    }

    public bool ContainWalkablePos(Vector3Int pos) => TileDatas.Where(x => x.IsWalkable).Select(x => x.GridPosition).Contains(pos);

    public HashSet<Vector3Int> GetOccupiedCells(List<Unit> units, Unit ignoreUnit = null)
    {
        var occupiedCells = new HashSet<Vector3Int>();

        foreach (Unit unit in units)
        {
            if (unit == null || unit == ignoreUnit || !unit.IsAlive)
            {
                continue;
            }

            occupiedCells.Add(unit.CurrentGridPosition);
        }

        return occupiedCells;
    }

    public bool IsWithinMoveRange(Vector3Int start, Vector3Int target, int moveRange, ISet<Vector3Int> blockedCells = null)
    => GetReachableTiles(start, moveRange, blockedCells).Contains(target);

    public IEnumerable<Vector3Int> GetReachableTiles(Vector3Int origin, int moveRange, ISet<Vector3Int> blockedCells = null)
    {
        var result = new List<Vector3Int>();
        var frontier = new Queue<(Vector3Int pos, int cost)>();
        var visited = new HashSet<Vector3Int>();

        frontier.Enqueue((origin, 0));
        visited.Add(origin);

        while (frontier.Count > 0)
        {
            var (current, cost) = frontier.Dequeue();
            if (cost > moveRange) continue;

            result.Add(current);

            foreach (var neighbor in GetNeighbors(current, blockedCells))
            {
                if (visited.Contains(neighbor)) continue;
                visited.Add(neighbor);
                frontier.Enqueue((neighbor, cost + 1));
            }
        }

        result.RemoveAt(0);
        return result;
    }

    private IEnumerable<Vector3Int> GetNeighbors(Vector3Int position, ISet<Vector3Int> blockedCells = null)
    {
        foreach (var dir in Directions)
        {
            var neighbor = position + dir;
            if (walkablePositions.Contains(neighbor) && (blockedCells == null || !blockedCells.Contains(neighbor)))
                yield return neighbor;
        }
    }

    public List<Vector3Int> FindPathToTarget(Vector3Int start, Vector3Int target, ISet<Vector3Int> blockedCells = null)
    {
        if (blockedCells != null && blockedCells.Contains(target))
        {
            return null;
        }

        var openSet = new List<PathNode>();
        var closedSet = new HashSet<Vector3Int>();
        var allNodes = new Dictionary<Vector3Int, PathNode>();

        var startNode = new PathNode(start, 0, GetManhattanDistance(start, target), null);
        openSet.Add(startNode);
        allNodes[start] = startNode;

        while (openSet.Count > 0)
        {
            var current = openSet.OrderBy(n => n.FCost).ThenBy(n => n.HCost).First();
            openSet.Remove(current);
            closedSet.Add(current.Position);

            if (current.Position == target)
                return ReconstructPath(current);

            foreach (var neighborPos in GetNeighbors(current.Position, blockedCells))
            {
                if (closedSet.Contains(neighborPos)) continue;

                int tentativeG = current.GCost + 1;

                if (!allNodes.TryGetValue(neighborPos, out var neighbor))
                {
                    neighbor = new PathNode(neighborPos, tentativeG, GetManhattanDistance(neighborPos, target), current);
                    allNodes[neighborPos] = neighbor;
                    openSet.Add(neighbor);
                }
                else if (tentativeG < neighbor.GCost)
                {
                    neighbor.GCost = tentativeG;
                    neighbor.Parent = current;
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }
    private static int GetManhattanDistance(Vector3Int a, Vector3Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    private static List<Vector3Int> ReconstructPath(PathNode endNode)
    {
        var path = new List<Vector3Int>();
        for (var node = endNode; node != null; node = node.Parent)
            path.Add(node.Position);
        path.Reverse();
        return path;
    }
}


public class PathNode
{
    public Vector3Int Position;
    public int GCost;
    public int HCost;
    public int FCost => GCost + HCost;
    public PathNode Parent;

    public PathNode(Vector3Int position, int gCost, int hCost, PathNode parent)
    {
        Position = position;
        GCost = gCost;
        HCost = hCost;
        Parent = parent;
    }
}