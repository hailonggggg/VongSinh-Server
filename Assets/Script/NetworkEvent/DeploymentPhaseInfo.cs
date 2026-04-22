using System;
using System.Collections.Generic;

[Serializable]
public class DeploymentPhaseInfo
{
    public List<int> DeployedUnitIds;
    public List<TileData> tiles;
}