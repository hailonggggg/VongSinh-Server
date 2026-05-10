using Fusion;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
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

    private readonly Dictionary<int, BattlePlayer> playersById;
    private readonly Client[] playerClients;
    private readonly BattleConfig config = new();
    private readonly HashSet<int> allowedCharacterSelectableIds;

    private int playerTurnIndex = 0;
    private float currentCountDown = 0f;
    private int lastBroadcastCountDownSecond = -1;
    private BattlePlayer currentTurnPlayer;
    private Map currentMap;

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
            StartDeploymentPhase();
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

        if (playersById.Values.All(p => p.DeployedUnitIds.Count >= CurrentTurnCount))
        {
            CurrentTurnCount++;
        }

        currentTurnPlayer.ResetState();
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
        playerTurn.HandleTurnStart(this, config.HasBanPhase);
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
        BattlePlayer battlePlayer = GetPlayer(client.PlayerRef);
        if (battlePlayer == null)
        {
            return;
        }

        if (!battlePlayer.TryGetDeployedUnitIdAt(placeUnit.IndexSelected, out int unitId))
        {
            return;
        }

        if (!battlePlayer.TryGetUnit(unitId, out Unit unit))
        {
            Master.Instance.CharactersById.TryGetValue(unitId, out Unit character);
            if (character == null)
            {
                ServerNetwork.Instance.SendToClient(client, Service.ShowNotification($"Đơn vị {unitId} không tồn tại"));
                return;
            }
            character.CurrentGridPosition = placeUnit.PlacedPosition;
            battlePlayer.AddUnit(character.Clone());
        }

        if (unit == null) return;

        if (!currentMap.IsValidSpawnPointPosition(battlePlayer.IsLeftSide, placeUnit.PlacedPosition))
        {
            battlePlayer.RemoveUnit(unitId);
            ServerNetwork.Instance.SendToClients(
                Service.RemoveUnit(unitId, battlePlayer.Client.PlayerRef.PlayerId),
                playerClients);

            return;
        }

        unit.CurrentGridPosition = placeUnit.PlacedPosition;

        ServerNetwork.Instance.SendToClients(
            Service.PlaceUnitResult(unit.Id, placeUnit.IndexSelected, placeUnit.PlacedPosition, battlePlayer.Client.PlayerRef.PlayerId),
            playerClients);
    }

    public bool TryHandleUnitDeploySelectedSkill(PlayerRef playerRef, UnitDeploySelectedSkillRequest request)
    {
        if (request == null || State != BattleState.Deployment)
        {
            return false;
        }

        BattlePlayer battlePlayer = GetPlayer(playerRef);
        if (battlePlayer == null)
        {
            return false;
        }

        if (!battlePlayer.TryGetUnit(request.CharId, out Unit unit))
        {
            return false;
        }

        SkillLoadoutType loadoutType = (SkillLoadoutType)request.Type;
        if (unit.GetListSkillLoadout(loadoutType).Contains(request.SkillId))
        {
            unit.RemoveSkillEquipped(request.SkillId, loadoutType);
            ServerNetwork.Instance.SendToClient(battlePlayer.Client, Service.SendUnitDeploySelectedSkill(unit.Id, request.SkillId, request.Type, false));
            return true;
        }

        if (unit.TryAssignSkill(request.SkillId, loadoutType))
        {
            ServerNetwork.Instance.SendToClient(battlePlayer.Client, Service.SendUnitDeploySelectedSkill(unit.Id, request.SkillId, request.Type, true));
            return true;
        }

        return false;
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
        State = BattleState.Combat;
        ServerNetwork.Instance.SendToClients(Service.StartCombatPhase(), playerClients);
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

    public void HandleUnitMove(Client client, int unitId, Vector3Int targetCell)
    {
        BattlePlayer player = GetPlayer(client.PlayerRef);
        if (player == null)
        {
            return;
        }
        if (!player.UnitCombats.TryGetValue(unitId, out Unit unit))
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Bạn không có nhân vật này trong đội hình."));
            return;
        }
        if (!currentMap.ContainWalkablePos(targetCell))
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Vị trí không hợp lệ."));
            return;
        }
        List<Unit> units = playersById.Values.SelectMany(x => x.UnitCombats.Values).ToList();
        HashSet<Vector3Int> blockCells = currentMap.GetOccupiedCells(units);
        if (!currentMap.IsWithinMoveRange(unit.CurrentGridPosition, targetCell, unit.MoveRange, blockCells))
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Vị trí đến nằm ngoài phạm vi."));
            return;
        }

        List<Vector3Int> paths = currentMap.FindPathToTarget(unit.CurrentGridPosition, targetCell, blockCells);
        if (paths == null || paths.Count == 0)
        {
            return;
        }
        unit.CurrentGridPosition = targetCell;
        ServerNetwork.Instance.SendToClients(Service.UnitMove(client.PlayerRef.PlayerId, unit.Id, paths), playerClients);
    }



    #endregion
}
