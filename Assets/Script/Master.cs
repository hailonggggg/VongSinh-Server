using System;
using System.Collections.Generic;
using System.IO;
using Assets.Script.System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class Master : MonoBehaviour
{
    public static Master Instance;
    public TacticalSOExportData TacticalSOExportData;
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

        Debug.Log($"Loaded: {TacticalSOExportData.Characters.Count} characters, {TacticalSOExportData.Skills.Count} skills, {TacticalSOExportData.Maps.Count} maps");
    }

    public Map LoadMap(int mapIndexSelected)
    {
        Map map = new();
        map.LoadData(TacticalSOExportData.Maps[mapIndexSelected]);
        return map;
    }
}


[Serializable]
public class TacticalSOExportData
{
    public List<CharacterDataJsonData> Characters = new List<CharacterDataJsonData>();
    [SerializeReference] public List<SkillJsonData> Skills = new List<SkillJsonData>();
    public List<PassiveJsonData> Passives = new List<PassiveJsonData>();
    public List<MapDataJsonData> Maps = new List<MapDataJsonData>();
}

[Serializable]
public class UnitDataJsonData
{
    public string AssetName;
    public string DisplayName;
    public int MaxHP;
    public int MoveRange;
}

[Serializable]
public class CharacterDataJsonData : UnitDataJsonData
{
    public int Id;
    public bool IsYuanUser;
    public bool IsAvailable;
    public int InitialSkillPoint;
    public List<int> SkillIds;
    public List<int> PassiveIds;
}

[Serializable]
public class PassiveJsonData
{
    public int Id;
    public string AssetName;
    public string PassiveName;
    public string Description;
    public List<string> ConditionTypeNames;
    public List<string> EffectTypeNames;
}


[Serializable]
public class MapDataJsonData
{
    public int Id;
    public string AssetName;
    public string PlayerSpawnFacing;
    public string EnemySpawnFacing;
    public List<TileDataJsonData> Tiles;
}

[Serializable]
public class TileDataJsonData
{
    public Vector3Int GridPosition;
    public bool IsWalkable;
    public bool IsSpawnPoint;
    public bool IsOpponentSpawnPoint;
}

[Serializable]
public class SkillJsonData
{
    public int Id;
    public string AssetName;
    public string SkillType;
    public string SkillName;
    public string Description;
    public bool IsDirectional;
    public bool IsUnlocked;
    public int Damage;
    public int CritRate;
    public string SkillOrigin;
    public int MoveRange;
    public int CooldownTurns;
    public int ActionPointCost;
    public int YuanLiCost;
    public int SkillPointCost;
    public string AnimationTrigger;
    public List<string> BuffTypeNames;
    public List<string> DebuffTypeNames;
    public List<SkillStageJsonData> Stages;
}

[Serializable]
public class SkillStageJsonData
{
    public string NameEffect;
}
