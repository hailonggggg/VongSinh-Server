
using System.Numerics;
using UnityEngine;

public class Unit
{
    public CharacterDataJsonData Data;
    public Vector3Int CurrentGridPosition;

    public Unit(CharacterDataJsonData data, Vector3Int position)
    {
        Data = data;
        CurrentGridPosition = position;
    }
}
