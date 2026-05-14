using Fusion;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;

public class Battle
{
    public enum BattleState
    {
        WaitingForSceneLoad,
        BanPick,
        Deployment,
        Combat,
        Finished
    }

    public int BattleId { get; }
    public int RoomId { get; }
    public float CurrentCountDown => currentCountDown;
    public int CurrentTurnCount = 1;
    public BattleState State { get; private set; }
    public Client[] PlayerClients => playerClients;
    public BattleConfig Config => config;
    public Map CurrentMap => currentMap;
    public Dictionary<int, BattlePlayer> PlayersById => playersById;

    private readonly Dictionary<int, BattlePlayer> playersById;
    private readonly Client[] playerClients;
    private readonly BattleConfig config = new();
    private readonly HashSet<int> allowedCharacterSelectableIds;

    private int playerTurnIndex = 0;
    private int lastBroadcastCountDownSecond = -1;
    private bool isSendDeploymentInfo;
    private float currentCountDown = 0f;
    private Map currentMap;
    private BattlePlayer currentTurnPlayer;

    public Battle(int battleId, int roomId, IEnumerable<BattlePlayer> players)
    {
        BattleId = battleId;
        RoomId = roomId;
        State = BattleState.WaitingForSceneLoad;
        playersById = players.ToDictionary(player => player.Client.PlayerRef.PlayerId);
        playerClients = players.Select(player => player.Client).ToArray();
        allowedCharacterSelectableIds = new HashSet<int>(config.AllowCharacterSelectables);
    }

    public void Tick(float deltaTime)
    {
        if (State == BattleState.BanPick)
        {
            UpdateBanPickState(deltaTime);
        }
        else if (State == BattleState.Deployment)
        {
            UpdateDeploymentState(deltaTime);
        }
        else if (State == BattleState.Combat)
        {
            UpdateCombatState(deltaTime);
        }
    }
    public bool TryMarkSceneLoaded(PlayerRef playerRef)
    {
        if (State != BattleState.WaitingForSceneLoad)
        {
            return false;
        }

        BattlePlayer player = GetPlayer(playerRef);
        if (player == null || !player.MarkSceneLoaded())
        {
            return false;
        }

        if (playersById.Values.All(x => x.IsSceneLoaded))
        {
            State = BattleState.BanPick;
        }

        return true;
    }

    #region BanPick State

    public bool IsReadyToStart()
    {
        return State == BattleState.BanPick;
    }

    public bool HandleUnitDeploySelected(PlayerRef playerRef, int unitDeployId)
    {
        BattlePlayer player = GetPlayer(playerRef);
        if (player == null || State != BattleState.BanPick)
        {
            return false;
        }

        if (currentTurnPlayer == null || !currentTurnPlayer.Client.PlayerRef.Equals(playerRef))
        {
            return false;
        }

        if (!allowedCharacterSelectableIds.Contains(unitDeployId))
        {
            return false;
        }

        if (!player.ApplyUnitDeploy(unitDeployId))
        {
            return false;
        }

        ServerNetwork.Instance.SendToClients(Service.SendBattlePlayerInfo(player), playerClients);

        if (playersById.Values.All(x => x.HasReachedDeployLimit(config.AllowCharacterSelectables.Length)))
        {
            LoadGameData();
            return true;
        }

        ProcessPlayersTurn();
        return true;
    }

    private void LoadGameData()
    {
        foreach (BattlePlayer player in playersById.Values)
        {
            player.Client.PendingPacket.Enqueue(() =>
            {
                player.MarkGameDataLoaded();
                if (playersById.Values.All(x => x.IsGameDataLoaded))
                {
                    StartDeploymentPhase();
                }
            });
        }

        ServerNetwork.Instance.SendToClients(
            Service.SendGameData(new GameDataResponse
            {
                GameData = JsonConvert.SerializeObject(Master.Instance.TacticalSOExportData)
            }), playerClients);
    }

    public void HandleBanPickSelected(PlayerRef playerRef, int unitBanId)
    {
        BattlePlayer player = GetPlayer(playerRef);
        if (player == null || State != BattleState.BanPick)
        {
            return;
        }

        if (currentTurnPlayer == null || !currentTurnPlayer.Client.PlayerRef.Equals(playerRef))
        {
            return;
        }

        if (!allowedCharacterSelectableIds.Contains(unitBanId))
        {
            return;
        }

        if (!player.ApplyUnitBan(unitBanId))
        {
            return;
        }

        player.IsBannedOtherUnit = true;
        ProcessPlayersTurn();
    }

    public void HandlePlayerTurnDone()
    {
        if (currentTurnPlayer == null || playersById.Values.Count == 0)
        {
            return;
        }
        if (State == BattleState.BanPick)
        {
            if (playersById.Values.All(p => p.DeployedUnitIds.Count >= CurrentTurnCount))
            {
                CurrentTurnCount++;
            }
            currentTurnPlayer.ResetState();
        }
        else if (State == BattleState.Combat)
        {
            CurrentTurnCount++;
        }
        playerTurnIndex = (playerTurnIndex + 1) % playersById.Values.Count;
        ProcessPlayersTurn();
    }

    public void BroadcastBanPickInfo()
    {
        bool shouldStartTurn = currentTurnPlayer == null;
        if (shouldStartTurn)
        {
            playerTurnIndex = Random.Range(0, playersById.Values.Count);
        }

        RoomSystem.TryGetRoomById(RoomId, out Room room);
        currentMap = Master.Instance.LoadMap(room.MapIndexSelected);

        BattlePlayerInfo[] playerInfos = playersById.Values.Select(player => new BattlePlayerInfo
        {
            Name = player.Name,
            DeployedUnitIds = player.DeployedUnitIds.ToList(),
            BannedUnitIds = player.BannedUnitIds.ToList()
        }).ToArray();

        foreach (BattlePlayer battlePlayer in playersById.Values)
        {
            Client battleClient = battlePlayer.Client;
            if (battleClient == null)
            {
                continue;
            }

            ServerNetwork.Instance.SendToClient(battleClient, Service.SendBanPickStartInfo(new BattleBanPickInfo
            {
                IsLocalPlayerOnLeftSide = battlePlayer.IsLeftSide,
                HasBanPhase = config.HasBanPhase,
                MapIndexSelected = room.MapIndexSelected,
                MaxUnitsPerPlayer = config.MaxUnitsPerPlayer,
                AllowCharacterSelectables = config.AllowCharacterSelectables,
                Players = playerInfos
            }));
        }

        if (shouldStartTurn)
        {
            ProcessPlayersTurn();
        }
    }

    private void UpdateBanPickState(float deltaTime)
    {
        currentCountDown = Mathf.Max(0f, currentCountDown - deltaTime);
        BroadcastTurnCountDownIfNeeded();
        if (currentCountDown > 0f)
        {
            return;
        }

        HandlePlayerTurnDone();
    }

    private void ProcessPlayersTurn()
    {
        BattlePlayer playerTurn = playersById.Values.ElementAt(playerTurnIndex);
        if (currentTurnPlayer != playerTurn)
        {
            currentCountDown = config.TurnTimeLimit;
            lastBroadcastCountDownSecond = -1;
        }

        currentTurnPlayer = playerTurn;
        BroadcastTurnCountDownIfNeeded(forceBroadcast: true);
        if (State == BattleState.BanPick)
        {
            playerTurn.HandleBanPickTurnStart(this, config.HasBanPhase);
        }
        else if (State == BattleState.Combat)
        {
            playerTurn.HandleCombatTurnStart(this);
        }
    }

    private void BroadcastTurnCountDownIfNeeded(bool forceBroadcast = false)
    {
        int currentSecond = Mathf.CeilToInt(currentCountDown);
        if (!forceBroadcast && currentSecond == lastBroadcastCountDownSecond)
        {
            return;
        }

        lastBroadcastCountDownSecond = currentSecond;
        ServerNetwork.Instance.SendToClients(
            Service.SendTimeCountDown(currentSecond),
            playerClients);
    }

    #endregion

    #region Deployment State

    private void UpdateDeploymentState(float deltaTime)
    {
        currentCountDown = Mathf.Max(0f, currentCountDown - deltaTime);
        BroadcastTurnCountDownIfNeeded();
        if (currentCountDown > 0f)
        {
            return;
        }
        StartCombatPhase();
    }

    private void StartDeploymentPhase()
    {
        if (isSendDeploymentInfo)
        {
            return;
        }
        isSendDeploymentInfo = true;
        currentCountDown = config.DeploymentTime;
        State = BattleState.Deployment;
        foreach (BattlePlayer battlePlayer in playersById.Values)
        {
            if (battlePlayer.Client == null)
            {
                continue;
            }


            ServerNetwork.Instance.SendToClient(battlePlayer.Client, Service.LoadDeploymentPhase(new DeploymentPhaseInfo
            {
                DeployedUnitIds = battlePlayer.DeployedUnitIds.ToList(),
                tiles = currentMap.TileDatas,
                SpawnTiles = battlePlayer.IsLeftSide ? currentMap.LeftTiles : currentMap.rightTiles
            }));
        }
    }

    public void SetUnitPlaced(Client client, PlaceUnit placeUnit)
    {
        BattlePlayer player = GetPlayer(client.PlayerRef);
        if (player == null)
        {
            return;
        }
        player.SetUnitPlaced(this, placeUnit);
    }

    public bool TryHandleUnitDeploySelectedSkill(PlayerRef playerRef, UnitDeploySelectedSkillRequest request)
    {
        if (request == null || State != BattleState.Deployment)
        {
            return false;
        }

        BattlePlayer player = GetPlayer(playerRef);
        if (player == null)
        {
            return false;
        }
        return player.TryHandleUnitDeploySelectedSkill(this, request);
    }

    private BattlePlayer GetPlayer(PlayerRef playerRef)
    {
        playersById.TryGetValue(playerRef.PlayerId, out BattlePlayer player);
        return player;
    }

    #endregion


    #region CombatPhase

    public void StartCombatPhase()
    {
        if (State == BattleState.Combat)
        {
            return;
        }
        foreach (var player in playersById.Values)
        {
            player.InitializeUnit();
        }
        State = BattleState.Combat;
        ResetTurnState();
        ServerNetwork.Instance.SendToClients(Service.StartCombatPhase(), playerClients);
        ProcessPlayersTurn();
    }

    public bool TryMarkSetupDeploymentComplete(Client client)
    {
        if (State != BattleState.Deployment)
        {
            return false;
        }
        BattlePlayer player = GetPlayer(client.PlayerRef);
        if (player == null || !player.MarkSetupDeploymentDone())
        {
            return false;
        }

        if (playersById.Values.All(x => x.DoneSetupDeployment))
        {
            return true;
        }
        return false;
    }

    public void HandleUnitMove(Client client, int unitId, Vector3Int currentCell, Vector3Int targetCell)
    {
        BattlePlayer player = GetPlayer(client.PlayerRef);
        if (player == null)
        {
            return;
        }

        player.HandleUnitMove(this, unitId, currentCell, targetCell);
    }

    private void ResetTurnState()
    {
        playerTurnIndex = 0;
        currentCountDown = 0;
        currentTurnPlayer = null;
    }

    public void HandleActionComplete(Client client)
    {
        if (currentTurnPlayer == null)
        {
            return;
        }
        if (currentTurnPlayer.Client.PlayerRef.PlayerId != client.PlayerRef.PlayerId)
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Không phải lượt của bạn nhưng bạn đang cố kết thúc lượt ?"));
            return;
        }
        if (!currentTurnPlayer.ApSystem.IsEmpty)
        {
            return;
        }
        HandlePlayerTurnDone();
    }

    private void UpdateCombatState(float deltaTime)
    {
        currentCountDown = Mathf.Max(0f, currentCountDown - deltaTime);
        BroadcastTurnCountDownIfNeeded();
        if (currentCountDown > 0f)
        {
            return;
        }

        HandlePlayerTurnDone();
    }

    public void HandleUseSkill(Client client, UseSkillRequest request)
    {
        if (request == null)
        {
            return;
        }

        BattlePlayer player = GetPlayer(client.PlayerRef);
        if (player == null)
        {
            return;
        }
        player.HandeUseSkill(this, request);
    }

    public void HandleOnFrameHit(Client client)
    {
        BattlePlayer player = GetPlayer(client.PlayerRef);
        if (player == null)
        {
            return;
        }

        if (currentTurnPlayer == null || currentTurnPlayer.ListUnitHavePendingDamage == null)
        {
            return;
        }

        if (!ReferenceEquals(player, currentTurnPlayer))
        {
            return;
        }

        foreach (Unit unit in currentTurnPlayer.ListUnitHavePendingDamage)
        {
            unit.ApplyPendingDamage();
        }
        currentTurnPlayer.ListUnitHavePendingDamage.Clear();
        if (currentTurnPlayer.ApSystem.IsEmpty)
        {
            HandleActionComplete(client);
        }
    }

    public BattleContext CreateBattleContext(BattlePlayer player, Unit unit)
    {
        return new BattleContext
        {
            Map = currentMap,
            ActionPointSystem = player.ApSystem,
            YuanPressureSystem = player.YuanPressureSystem,
            Allies = player.UnitCombats.Values.Where(u => u != unit).ToList(),
            Enemies = playersById.Values.Where(p => p != player).SelectMany(p => p.UnitCombats.Values).ToList(),
        };
    }


    #endregion
}

public class BattleContext
{
    public Map Map;
    public ActionPointSystem ActionPointSystem;
    public YuanPressureSystem YuanPressureSystem;
    public List<Unit> Allies;
    public List<Unit> Enemies;
}
public class SkillHandler
{
    public static Vector3Int GetSkillPreviewDirection(bool isDirectional, Unit unit, Vector3Int targetPosition)
    {
        if (isDirectional)
        {
            return GetCardinalDirection(unit.CurrentGridPosition, targetPosition, Vector3Int.zero);
        }
        return unit.FacingDirection;
    }

    public static List<Unit> GetAffectedEnemyUnits(List<SkillTileData> selectedTileAffectedTargets, BattleContext battleContext, Vector3Int targetCell)
    {
        List<Unit> affectedUnits = new();
        foreach (SkillTileData tileData in selectedTileAffectedTargets)
        {
            Vector3Int cell = tileData.offset + targetCell;
            affectedUnits.AddRange(battleContext.Enemies.Where(a => a.CurrentGridPosition == cell));
        }
        return affectedUnits;
    }

    private static Vector3Int GetCardinalDirection(Vector3Int originCell, Vector3Int targetCell, Vector3Int fallbackDirection)
    {
        Vector3Int delta = targetCell - originCell;
        if (delta == Vector3Int.zero)
        {
            return fallbackDirection;
        }

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return delta.x > 0 ? Vector3Int.right : Vector3Int.left;
        }

        return delta.y > 0 ? Vector3Int.up : Vector3Int.down;
    }
}