using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class CharacterSystem : BaseSystem
{
    public static Dictionary<int, CharacterStats> CharacterStats => characterStats.ToDictionary(x => x.Key, x => x.Value);
    public static Dictionary<int, CharacterSkill> CharacterSkills => characterSkills.ToDictionary(x => x.Key, x => x.Value);
    public static Dictionary<int, CharacterBasicAttack> CharacterBasicAttacks => characterBasicAttacks.ToDictionary(x => x.Key, x => x.Value);
    private static Dictionary<int, CharacterStats> characterStats;
    private static Dictionary<int, CharacterSkill> characterSkills;
    private static Dictionary<int, CharacterBasicAttack> characterBasicAttacks;
    public CharacterSystem()
    {
    }

    public async Task FetchCharacterStatsAndSkill()
    {
        characterStats = await ApiService.FetchAllCharacterStats();
        characterSkills = await ApiService.FetchAllCharacterSkill();
        characterBasicAttacks = await ApiService.FetchAllCharacterBasicAttacks();
        Debug.Log($"Done fetch ChacracterStats, count:{characterStats.Count}");
        Debug.Log($"Done fetch ChacracterSkill, count:{characterSkills.Count}");
        Debug.Log($"Done fetch ChacracterBasicAttack, count:{characterBasicAttacks.Count}");
    }
}
