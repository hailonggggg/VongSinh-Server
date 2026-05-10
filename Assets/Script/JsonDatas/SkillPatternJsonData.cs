using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class SkillPatternJsonData
{
    public Vector3Int PreviewTargetOffset = Vector3Int.zero;
    public float RedDamageMultiplier = 0.25f;
    public float OrangeDamageMultiplier = 0.5f;
    public float YellowDamageMultiplier = 1f;
    public List<SkillTileData> TargetTiles = new();
}
