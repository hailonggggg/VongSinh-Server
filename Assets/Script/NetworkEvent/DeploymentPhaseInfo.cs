using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

[Serializable]
public class DeploymentPhaseInfo
{
    public List<int> DeployedUnitIds;
    public List<TileData> tiles;
    public List<TileData> SpawnTiles;
}