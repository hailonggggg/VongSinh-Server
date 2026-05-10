using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterDataJsonData : UnitDataJsonData
{
    public bool IsYuanUser;
    public bool IsAvailable;
    public int InitialSkillPoint;
    public List<int> basicAttackSkillIds;
    public List<int> yuanSkillIds;
    public List<int> PassiveIds;
}

