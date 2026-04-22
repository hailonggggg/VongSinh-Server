using System;
using UnityEngine;

[Serializable]
public class UnitPlaced
{
    public int UnitId;
    public int Index;
    public Vector3Int PlacedPosition;
    public int PlayerRefId;
}
