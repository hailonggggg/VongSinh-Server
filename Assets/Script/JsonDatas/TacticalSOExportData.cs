using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TacticalSOExportData
{
    public List<CharacterDataJsonData> Characters = new();
    public List<BasicAttackSkillJsonData> BasicAttackSkillJsonDatas = new();
    public List<YuanSkillJsonData> YuanSkillJsonDatas = new();
    public List<StatusEffectJsonData> StatusEffects = new();
    public List<PassiveJsonData> Passives = new();
    public List<MapDataJsonData> Maps = new();
}
