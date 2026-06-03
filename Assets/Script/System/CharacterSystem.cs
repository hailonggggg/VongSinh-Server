using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class CharacterSystem : BaseSystem
{
    public static Dictionary<int, CharacterStats> CharacterStats {get; private set; } = new();
    public static Dictionary<int, CharacterSkill> CharacterSkills { get; private set; } = new();
    public static Dictionary<int, CharacterBasicAttack> CharacterBasicAttacks { get; private set; } = new();
    public static Dictionary<int, CharacterPvP> CharacterPvPs { get; private set; } = new();
    public CharacterSystem()
    {
    }

    public async Task FetchCharacterStatsAndSkill()
    {
        CharacterStats = await ApiService.FetchAllCharacterStats();
        CharacterSkills = await ApiService.FetchAllCharacterSkill();
        CharacterBasicAttacks = await ApiService.FetchAllCharacterBasicAttacks();
        Debug.Log($"Done fetch ChacracterStats, count:{CharacterStats.Count}");
        Debug.Log($"Done fetch ChacracterSkill, count:{CharacterSkills.Count}");
        Debug.Log($"Done fetch ChacracterBasicAttack, count:{CharacterBasicAttacks.Count}");
    }

    public async Task FetchCharacterPvPs()
    {
        CharacterPvPs = await ApiService.GetAllCharacterPvP();
        Debug.Log($"Done fetch CharacterPvP, count:{CharacterPvPs.Count}");
    }
}
