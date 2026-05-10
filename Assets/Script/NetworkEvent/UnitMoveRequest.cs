using System;
using UnityEngine;

[Serializable]
public class UnitMoveRequest
{
    public int UnitId;
    public Vector3Int TargetCell;
}
