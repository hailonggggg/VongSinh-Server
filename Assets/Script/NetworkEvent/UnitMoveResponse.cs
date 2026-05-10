using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UnitMoveResponse
{
    public int PlayerId;
    public int UnitId;
    public List<Vector3Int> Paths;
}
