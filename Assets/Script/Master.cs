using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Assets.Script.System;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Master : MonoBehaviour
{
    public static Master Instance;

    /// <summary>
    /// Set to true by the MasterDataEditor whenever a stat is edited at runtime.
    /// When true, <see cref="SaveTacticalSOData"/> is invoked on application quit.
    /// </summary>
    public static bool TacticalDataDirty;

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
        try
        {
            await characterSystem.FetchCharacterStatsAndSkill();
            await characterSystem.FetchCharacterPvPs();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error fetching character data: {ex.Message}");
        }
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

    private void OnApplicationQuit()
    {
        if (TacticalDataDirty)
        {
            SaveTacticalSOData();
        }
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
        TextAsset data = Resources.Load<TextAsset>("tactical_so_data");

        if (data == null)
        {
            return;
        }

        TacticalSOExportData = JsonConvert.DeserializeObject<TacticalSOExportData>(data.text);

        foreach (var statusEffectData in TacticalSOExportData.StatusEffects)
        {
            if (StatusEffectByIds.ContainsKey(statusEffectData.Id))
                continue;

            StatusEffectByIds[statusEffectData.Id] = StatusEffect.FromJson(statusEffectData);
        }

        foreach (var characterData in TacticalSOExportData.Characters)
        {
            ApplyCharacterFromDatabase(characterData);

            if (CharactersById.ContainsKey(characterData.Id))
                continue;

            CharactersById[characterData.Id] = new Unit(characterData);
        }

        foreach (var skillData in TacticalSOExportData.BasicAttackSkillJsonDatas)
        {
            ApplyBasicAttackFromDatabase(skillData);

            if (BasicAttackSkillsById.ContainsKey(skillData.Id))
                continue;

            skillData.IsUnlocked = true;
            BasicAttackSkillsById[skillData.Id] = BasicAttackSkill.FromJson(skillData);
        }

        foreach (var yuanSkillData in TacticalSOExportData.YuanSkillJsonDatas)
        {
            ApplySkillFromDatabase(yuanSkillData);

            if (YuanSkillsById.ContainsKey(yuanSkillData.Id))
                continue;

            yuanSkillData.IsUnlocked = true;
            YuanSkillsById[yuanSkillData.Id] = YuanSkill.FromJson(yuanSkillData);
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

    private static void ApplySkillFromDatabase(YuanSkillJsonData yuanSkillData)
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
    }

    private static void ApplyCharacterFromDatabase(CharacterDataJsonData characterData)
    {
        if (CharacterSystem.CharacterStats.TryGetValue(characterData.Id, out CharacterStats characterStats))
        {
            characterData.MoveRange = characterStats.MoveRange;
            characterData.MaxHP = characterStats.MaxHealth;
        }
        if (CharacterSystem.CharacterPvPs.TryGetValue(characterData.Id, out CharacterPvP characterPvP))
        {
            characterData.DisplayName = characterPvP.Name;
        }
    }

    private static void ApplyBasicAttackFromDatabase(BasicAttackSkillJsonData skillData)
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
    }

    // private void ApplyYuanSkillStatsFromDatabase(YuanSkill yuanSkill)
    // {
    //     if (CharacterSystem.CharacterSkills.TryGetValue(yuanSkill.Id, out CharacterSkill characterSkill))
    //     {
    //         yuanSkill.SetStats(characterSkill);
    //     }
    // }

    // private void ApplyBasicAttackStatsFromDatabase(BasicAttackSkill basicAttackSkill)
    // {
    //     if (CharacterSystem.CharacterBasicAttacks.TryGetValue(basicAttackSkill.Id, out CharacterBasicAttack basicAttackStats))
    //     {
    //         basicAttackSkill.SetStats(basicAttackStats);
    //     }
    // }

    // private void ApplyCharacterStatsFromDatabase(Unit unit)
    // {
    //     if (CharacterSystem.CharacterStats.TryGetValue(unit.Id, out CharacterStats characterStats))
    //     {
    //         unit.SetCharacterStats(characterStats);
    //     }
    // }

    public Map LoadMap(int mapIndexSelected)
    {
        Map map = new();
        map.LoadData(TacticalSOExportData.Maps[mapIndexSelected]);
        return map;
    }

    /// <summary>
    /// Persists the current in-memory tactical data back to
    /// Assets/Resources/tactical_so_data.json. Character edits are already
    /// reflected because <c>Unit.Data</c> references the live JSON object;
    /// skills, status effects and passives are runtime copies, so their values
    /// are synced back into the JSON objects first.
    /// </summary>
    public void SaveTacticalSOData()
    {
        if (TacticalSOExportData == null)
        {
            Debug.LogWarning("[Master] No TacticalSOExportData loaded; nothing to save.");
            return;
        }

        foreach (var json in TacticalSOExportData.YuanSkillJsonDatas)
        {
            if (YuanSkillsById.TryGetValue(json.Id, out var rt))
                CopyRuntimeToJson(rt, json, 0);
        }
        foreach (var json in TacticalSOExportData.BasicAttackSkillJsonDatas)
        {
            if (BasicAttackSkillsById.TryGetValue(json.Id, out var rt))
                CopyRuntimeToJson(rt, json, 1);
        }
        foreach (var json in TacticalSOExportData.StatusEffects)
        {
            if (StatusEffectByIds.TryGetValue(json.Id, out var rt))
                CopyRuntimeToJson(rt, json, 0);
        }
        foreach (var json in TacticalSOExportData.Passives)
        {
            if (PassiveByIds.TryGetValue(json.Id, out var rt))
                CopyRuntimeToJson(rt, json, 0);
        }

        string path = Path.Combine(Application.dataPath, "Resources", "tactical_so_data.json");
        try
        {
            string content = JsonConvert.SerializeObject(TacticalSOExportData, Formatting.Indented);
            File.WriteAllText(path, content);
            TacticalDataDirty = false;
            Debug.Log($"[Master] Saved tactical data to {path}");
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Master] Failed to save tactical data: {ex.Message}");
        }
    }

    /// <summary>
    /// Copies values from a runtime object onto its serializable JSON twin by
    /// matching member names (case-insensitive). Only fields that exist on the
    /// JSON object are written, so JSON-only data (patterns, lists, stages) is
    /// preserved. Enums are converted to strings to match the JSON schema.
    /// </summary>
    private static void CopyRuntimeToJson(object runtime, object json, int depth)
    {
        if (runtime == null || json == null) return;

        Type runtimeType = runtime.GetType();
        foreach (FieldInfo jf in json.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (jf.IsInitOnly || jf.IsLiteral) continue;

            if (!TryGetRuntimeMember(runtime, runtimeType, jf.Name, out object rtValue, out Type rtType))
                continue;
            if (rtValue == null) continue;

            Type jt = jf.FieldType;

            // enum (runtime) -> string (json)
            if (jt == typeof(string) && rtType.IsEnum)
            {
                jf.SetValue(json, rtValue.ToString());
                continue;
            }

            // simple value copy
            if (IsSimpleSaveType(jt) && IsSimpleSaveType(rtType))
            {
                try { jf.SetValue(json, Convert.ChangeType(rtValue, jt)); }
                catch { /* ignore incompatible conversions */ }
                continue;
            }

            // one-level recursion into nested config objects (e.g. SkillInfo)
            if (depth > 0
                && jt.IsClass && jt != typeof(string) && !typeof(IEnumerable).IsAssignableFrom(jt)
                && rtType.IsClass && !typeof(IEnumerable).IsAssignableFrom(rtType))
            {
                object jChild = jf.GetValue(json);
                if (jChild != null)
                {
                    CopyRuntimeToJson(rtValue, jChild, depth - 1);
                }
            }
        }
    }

    private static bool TryGetRuntimeMember(object obj, Type type, string name, out object value, out Type memberType)
    {
        value = null;
        memberType = null;

        PropertyInfo prop = type.GetProperty(name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop != null && prop.CanRead)
        {
            value = prop.GetValue(obj);
            memberType = prop.PropertyType;
            return true;
        }

        FieldInfo field = type.GetField(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (field != null)
        {
            value = field.GetValue(obj);
            memberType = field.FieldType;
            return true;
        }

        return false;
    }

    private static bool IsSimpleSaveType(Type t)
    {
        if (t.IsEnum) return true;
        return t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(short)
            || t == typeof(byte) || t == typeof(float) || t == typeof(double)
            || t == typeof(bool) || t == typeof(string);
    }
}
