using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillPattern
{
    public int previewGridSize = 7;
    private Vector3Int previewTargetOffset = Vector3Int.zero;
    private float redDamageMultiplier = 0.25f;
    private float orangeDamageMultiplier = 0.5f;
    private float yellowDamageMultiplier = 1f;
    public List<SkillTileData> targetTiles = new List<SkillTileData>();


    public List<Vector3Int> GetAffectedTiles(Vector3Int originPosition, Vector3Int facingDirection = default)
    {
        NormalizeTargetTiles();

        List<Vector3Int> affectedTiles = new List<Vector3Int>();

        foreach (var targetTile in targetTiles)
        {
            Vector3Int rotatedOffset = RotateOffset(targetTile.offset, facingDirection);
            Vector3Int worldPosition = originPosition + rotatedOffset;
            affectedTiles.Add(worldPosition);
        }

        return affectedTiles;
    }

    public List<SkillTileData> GetAffectedTileData(Vector3Int facingDirection = default)
    {
        NormalizeTargetTiles();
        List<SkillTileData> affectedTiles = new List<SkillTileData>();
        foreach (var targetTile in targetTiles)
        {
            Vector3Int rotatedOffset = RotateOffset(targetTile.offset, facingDirection);
            SkillTileData rotatedTileData = targetTile;
            rotatedTileData.offset = rotatedOffset;
            affectedTiles.Add(rotatedTileData);
        }
        return affectedTiles;
    }

    public Vector3Int GetTargetCellOffset()
    {
        NormalizeTargetTiles();

        foreach (SkillTileData targetTile in targetTiles)
        {
            if (targetTile.IsTargetCell)
            {
                return targetTile.offset;
            }
        }

        return previewTargetOffset;
    }

    public Vector3Int GetTargetCellOffset(Vector3Int facingDirection)
    {
        return RotateOffset(GetTargetCellOffset(), facingDirection);
    }

    public bool TryGetTargetCellOffset(out Vector3Int targetOffset)
    {
        NormalizeTargetTiles();

        foreach (SkillTileData targetTile in targetTiles)
        {
            if (targetTile.IsTargetCell)
            {
                targetOffset = targetTile.offset;
                return true;
            }
        }

        targetOffset = Vector3Int.zero;
        return false;
    }

    private void NormalizeTileColors()
    {
        if (targetTiles == null)
        {
            return;
        }

        for (int i = 0; i < targetTiles.Count; i++)
        {
            SkillTileData tileData = targetTiles[i];
            SkillTileColor normalizedColor = GetNormalizedTileColor(tileData);
            if (tileData.tileColor == normalizedColor)
            {
                continue;
            }

            tileData.tileColor = normalizedColor;
            targetTiles[i] = tileData;
        }
    }

    private SkillTileColor GetNormalizedTileColor(SkillTileData tileData)
    {
        if (tileData.damageMultiplier <= 0f)
        {
            return SkillTileColor.None;
        }

        if (tileData.tileColor != SkillTileColor.None)
        {
            return tileData.tileColor;
        }

        return InferTileColorFromMultiplier(tileData.damageMultiplier);
    }

    private SkillTileColor InferTileColorFromMultiplier(float multiplier)
    {
        if (Mathf.Approximately(multiplier, redDamageMultiplier))
        {
            return SkillTileColor.Red;
        }

        if (Mathf.Approximately(multiplier, orangeDamageMultiplier))
        {
            return SkillTileColor.Orange;
        }

        if (Mathf.Approximately(multiplier, yellowDamageMultiplier))
        {
            return SkillTileColor.Yellow;
        }

        return SkillTileColor.None;
    }

    public void NormalizeTargetTiles()
    {
        targetTiles ??= new List<SkillTileData>();
        NormalizeTileColors();

        int firstTargetIndex = -1;
        for (int i = 0; i < targetTiles.Count; i++)
        {
            SkillTileData tileData = targetTiles[i];
            if (!tileData.IsTargetCell)
            {
                continue;
            }

            if (firstTargetIndex < 0)
            {
                firstTargetIndex = i;
                continue;
            }

            tileData.IsTargetCell = false;
            targetTiles[i] = tileData;
        }

        if (firstTargetIndex < 0 && previewTargetOffset != Vector3Int.zero)
        {
            int legacyTileIndex = FindTileIndex(previewTargetOffset);
            if (legacyTileIndex >= 0)
            {
                SkillTileData tileData = targetTiles[legacyTileIndex];
                tileData.IsTargetCell = true;
                targetTiles[legacyTileIndex] = tileData;
            }
            else
            {
                targetTiles.Add(new SkillTileData
                {
                    offset = previewTargetOffset,
                    IsTargetCell = true,
                    tileColor = SkillTileColor.None,
                    damageMultiplier = 0f
                });
            }
        }

        previewTargetOffset = Vector3Int.zero;
    }

    public void SetTargetCellOffset(Vector3Int targetOffset)
    {
        NormalizeTargetTiles();
        ClearTargetCell();

        if (targetOffset == Vector3Int.zero)
        {
            return;
        }

        int tileIndex = FindTileIndex(targetOffset);
        if (tileIndex >= 0)
        {
            SkillTileData tileData = targetTiles[tileIndex];
            tileData.IsTargetCell = true;
            targetTiles[tileIndex] = tileData;
            return;
        }

        targetTiles.Add(new SkillTileData
        {
            offset = targetOffset,
            IsTargetCell = true,
            tileColor = SkillTileColor.None,
            damageMultiplier = 0f
        });
    }

    public void ClearTargetCell()
    {
        if (targetTiles == null)
        {
            targetTiles = new List<SkillTileData>();
        }

        for (int i = targetTiles.Count - 1; i >= 0; i--)
        {
            SkillTileData tileData = targetTiles[i];
            if (!tileData.IsTargetCell)
            {
                continue;
            }

            if (Mathf.Approximately(tileData.damageMultiplier, 0f))
            {
                targetTiles.RemoveAt(i);
                continue;
            }

            tileData.IsTargetCell = false;
            targetTiles[i] = tileData;
        }

        previewTargetOffset = Vector3Int.zero;
    }

    private int FindTileIndex(Vector3Int offset)
    {
        if (targetTiles == null)
        {
            return -1;
        }

        for (int i = 0; i < targetTiles.Count; i++)
        {
            if (targetTiles[i].offset == offset)
            {
                return i;
            }
        }

        return -1;
    }

    private Vector3Int RotateOffset(Vector3Int offset, Vector3Int direction)
    {
        if (direction == Vector3Int.up)
        {
            return new Vector3Int(offset.x, offset.y, 0);
        }
        else if (direction == Vector3Int.down)
        {
            return new Vector3Int(-offset.x, -offset.y, 0);
        }
        else if (direction == Vector3Int.left)
        {
            return new Vector3Int(-offset.y, offset.x, 0);
        }
        else if (direction == Vector3Int.right)
        {
            return new Vector3Int(offset.y, -offset.x, 0);
        }
        else
        {
            return new Vector3Int(offset.x, offset.y, 0);
        }
    }

    public static SkillPattern FromJson(SkillPatternJsonData data)
    {
        return new SkillPattern
        {
            previewTargetOffset = data.PreviewTargetOffset,
            orangeDamageMultiplier = data.OrangeDamageMultiplier,
            redDamageMultiplier = data.RedDamageMultiplier,
            yellowDamageMultiplier = data.YellowDamageMultiplier,
            targetTiles = data.TargetTiles
        };
    }

}


