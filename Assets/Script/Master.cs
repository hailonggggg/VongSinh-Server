using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
    private AuthSystem authSystem;
    private RoomSystem roomSystem;
    private BattleSystem battleSystem;
    private AnnouncementSystem announcementSystem;
    private BundleSystem bundleSystem;
    private OrderSystem orderSystem;
    private InventorySystem inventorySystem;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        LoadTacticalSODataFromJson();
        authSystem = new AuthSystem();
        roomSystem = new RoomSystem();
        battleSystem = new BattleSystem();
        announcementSystem = new AnnouncementSystem();
        bundleSystem = new BundleSystem();
        orderSystem = new OrderSystem();
        inventorySystem = new InventorySystem();
    }

    void Update()
    {
        battleSystem?.Tick(Time.deltaTime);
    }


    public void ClearClientResource(Client client)
    {
        if (client == null || client.CurrentRoomId < 0)
        {
            return;
        }

        if (RoomSystem.TryGetRoomById(client.CurrentRoomId, out Room room))
        {
            roomSystem.LeaveRoom(client);
        }
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
            if (CharactersById.ContainsKey(characterData.Id))
                continue;

            CharactersById[characterData.Id] = new Unit(characterData);
        }
        foreach (var skillData in TacticalSOExportData.BasicAttackSkillJsonDatas)
        {
            skillData.IsUnlocked = true;
            if (BasicAttackSkillsById.ContainsKey(skillData.Id))
                continue;

            BasicAttackSkillsById[skillData.Id] = BasicAttackSkill.FromJson(skillData);
        }
        foreach (var yuanSkillData in TacticalSOExportData.YuanSkillJsonDatas)
        {
            yuanSkillData.IsUnlocked = true;
            if (YuanSkillsById.ContainsKey(yuanSkillData.Id))
                continue;

            YuanSkillsById[yuanSkillData.Id] = YuanSkill.FromJson(yuanSkillData);
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

    public Map LoadMap(int mapIndexSelected)
    {
        Map map = new();
        map.LoadData(TacticalSOExportData.Maps[mapIndexSelected]);
        return map;
    }
}
