using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Assets.Script.System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class Master : MonoBehaviour
{
    public static Master Instance;
    public TacticalSOExportData TacticalSOExportData;
    public Dictionary<int, Unit> CharactersById { get; private set; } = new();
    public Dictionary<int, YuanSkill> YuanSkillsById { get; private set; } = new();
    public Dictionary<int, BasicAttackSkill> BasicAttackSkillsById { get; private set; } = new();
    public Dictionary<int, StatusEffect> StatusEffectByIds { get; private set; } = new();
    public Dictionary<int, Passive> PassiveByIds { get; private set; } = new();
    public BattleConfig Config;
    private AuthSystem authSystem;
    private RoomSystem roomSystem;
    private BattleSystem battleSystem;
    private AnnouncementSystem announcementSystem;
    private BundleSystem bundleSystem;
    private OrderSystem orderSystem;
    private InventorySystem inventorySystem;
    private CharacterSystem characterSystem;

    void Awake()
    {
        Instance = this;
    }

    async void Start()
    {
        authSystem = new AuthSystem();
        roomSystem = new RoomSystem();
        battleSystem = new BattleSystem();
        announcementSystem = new AnnouncementSystem();
        bundleSystem = new BundleSystem();
        orderSystem = new OrderSystem();
        inventorySystem = new InventorySystem();
        characterSystem = new CharacterSystem();
        await characterSystem.FetchCharacterStatsAndSkill();
        LoadTacticalSODataFromJson();
    }

    private float matchmakingTimer = 0;

    void Update()
    {
        battleSystem?.Tick(Time.deltaTime);

        matchmakingTimer += Time.deltaTime;
        if (matchmakingTimer >= 1.0f)
        {
            RoomSystem.SendMatchmakingUpdates();
            matchmakingTimer = 0;
        }
        RoomSystem.TryCreatePendingMatch();
        RoomSystem.CheckPendingMatchTimeouts();
    }


    public void ClearClientResource(Client client)
    {
        if (client == null)
        {
            Debug.LogWarning("Cannot clear Resource because client is null");
            return;
        }
        if (client.CurrentBattleId != -1 && BattleSystem.TryGetBattleById(client.CurrentBattleId, out Battle battle))
        {
            battle.HandleLeaveBattle(client);
        }

        if (client.CurrentRoomId != -1 && RoomSystem.TryGetRoomById(client.CurrentRoomId, out _))
        {
            roomSystem.LeaveRoom(client);
        }
        RoomSystem.HandleCancelRandomMatch(client);
    }

    private void LoadTacticalSODataFromJson()
    {
        string filePath = Path.Combine(Application.dataPath, "tactical_so_data.json");

        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return;
        }

        string json = File.ReadAllText(filePath);
        TacticalSOExportData = JsonConvert.DeserializeObject<TacticalSOExportData>(json);

        foreach (var characterData in TacticalSOExportData.Characters)
        {
            if (CharacterSystem.CharacterStats.TryGetValue(characterData.Id, out CharacterStats characterStats))
            {
                characterData.MoveRange = characterStats.MoveRange;
                characterData.MaxHP = characterStats.MaxHealth;
            }

            if (CharactersById.ContainsKey(characterData.Id))
                continue;

            CharactersById[characterData.Id] = new Unit(characterData);
            ApplyCharacterStatsFromDatabase(CharactersById[characterData.Id]);
        }
        foreach (var skillData in TacticalSOExportData.BasicAttackSkillJsonDatas)
        {
            if (CharacterSystem.CharacterBasicAttacks.TryGetValue(skillData.Id, out CharacterBasicAttack attack))
            {
                skillData.SkillName = attack.Name;
                skillData.Description = attack.Description;
                skillData.Damage = attack.Damage;
                skillData.CritRate = attack.CritRate;
                if (skillData.NormalInfo != null && attack.NormalInfo != null)
                {
                    skillData.NormalInfo.ActionPointCost = attack.NormalInfo.ActionPointCost;
                    skillData.NormalInfo.SkillPointCost = attack.NormalInfo.SkillPointCost;
                    skillData.NormalInfo.YuanLiCost = attack.NormalInfo.YuanLiCost;
                }
                if (skillData.YuanInfo != null && attack.YuanInfo != null)
                {
                    skillData.YuanInfo.ActionPointCost = attack.YuanInfo.ActionPointCost;
                    skillData.YuanInfo.SkillPointCost = attack.YuanInfo.SkillPointCost;
                    skillData.YuanInfo.YuanLiCost = attack.YuanInfo.YuanLiCost;
                }
            }

            skillData.IsUnlocked = true;
            if (BasicAttackSkillsById.ContainsKey(skillData.Id))
                continue;

            BasicAttackSkillsById[skillData.Id] = BasicAttackSkill.FromJson(skillData);
            ApplyBasicAttackStatsFromDatabase(BasicAttackSkillsById[skillData.Id]);
        }
        foreach (var yuanSkillData in TacticalSOExportData.YuanSkillJsonDatas)
        {
            if (CharacterSystem.CharacterSkills.TryGetValue(yuanSkillData.Id, out CharacterSkill skill))
            {
                yuanSkillData.SkillName = skill.Name;
                yuanSkillData.Description = skill.Description;
                yuanSkillData.Damage = skill.Damage;
                yuanSkillData.SkillPointCost = skill.SP;
                yuanSkillData.YuanLiCost = skill.YuanPressure;
                yuanSkillData.CritRate = skill.CritRate;
            }
            yuanSkillData.IsUnlocked = true;
            if (YuanSkillsById.ContainsKey(yuanSkillData.Id))
                continue;

            YuanSkillsById[yuanSkillData.Id] = YuanSkill.FromJson(yuanSkillData);
            ApplyYuanSkillStatsFromDatabase(YuanSkillsById[yuanSkillData.Id]);
        }
        foreach (var statusEffectData in TacticalSOExportData.StatusEffects)
        {
            if (StatusEffectByIds.ContainsKey(statusEffectData.Id))
                continue;

            StatusEffectByIds[statusEffectData.Id] = StatusEffect.FromJson(statusEffectData);
        }
        foreach (var passiveData in TacticalSOExportData.Passives)
        {
            passiveData.IsUnlocked = true;
            if (PassiveByIds.ContainsKey(passiveData.Id))
                continue;

            PassiveByIds[passiveData.Id] = Passive.FromJson(passiveData);
        }

        Debug.Log(
            @$"Loaded: {CharactersById.Count} characters, 
            {BasicAttackSkillsById.Count + YuanSkillsById.Count} skills, 
            {TacticalSOExportData.Maps.Count} maps, 
            {StatusEffectByIds.Count} statusEffects
            {PassiveByIds.Count} Passives");
    }

    private void ApplyYuanSkillStatsFromDatabase(YuanSkill yuanSkill)
    {
        if (CharacterSystem.CharacterSkills.TryGetValue(yuanSkill.Id, out CharacterSkill characterSkill))
        {
            yuanSkill.SetStats(characterSkill);
        }
    }

    private void ApplyBasicAttackStatsFromDatabase(BasicAttackSkill basicAttackSkill)
    {
        if (CharacterSystem.CharacterBasicAttacks.TryGetValue(basicAttackSkill.Id, out CharacterBasicAttack basicAttackStats))
        {
            basicAttackSkill.SetStats(basicAttackStats);
        }
    }

    private void ApplyCharacterStatsFromDatabase(Unit unit)
    {
        if (CharacterSystem.CharacterStats.TryGetValue(unit.Id, out CharacterStats characterStats))
        {
            unit.SetCharacterStats(characterStats);
        }
    }

    public Map LoadMap(int mapIndexSelected)
    {
        Map map = new();
        map.LoadData(TacticalSOExportData.Maps[mapIndexSelected]);
        return map;
    }
}
