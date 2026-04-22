using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public IReadOnlyList<BattlePlayer> Players => players;


    private readonly List<BattlePlayer> players;
    private readonly BattleConfig config = new();


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
        this.players = players.ToList();
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

        BattlePlayer player = players.FirstOrDefault(x => x.PlayerRef == playerRef);
        if (player == null || !player.MarkSceneLoaded())
        {
            return false;
        }

        if (players.All(x => x.IsSceneLoaded))
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
        BattlePlayer player = players.FirstOrDefault(x => x.PlayerRef == playerRef);
        if (player == null || State != BattleState.BanPick)
        {
            return false;
        }

        if (!currentTurnPlayer.PlayerRef.Equals(playerRef))
        {
            return false;
        }

        if (!config.AllowCharacterSelectables.Contains(unitDeployId))
        {
            return false;
        }

        if (!player.ApplyUnitDeploy(unitDeployId))
        {
            return false;
        }
        ServerNetwork.Instance.SendToClients(Service.SendBattlePlayerInfo(player), players.Select(p => p.PlayerRef).ToArray());

        if (players.All(x => x.HasReachedDeployLimit(config.AllowCharacterSelectables.Length)))
        {
            StartDeploymentPhase();
            return true;
        }
        ProcessPlayersTurn();
        return true;
    }

    public void HandleBanPickSelected(PlayerRef playerRef, int unitBanId)
    {
        BattlePlayer player = players.FirstOrDefault(x => x.PlayerRef == playerRef);
        if (player == null || State != BattleState.BanPick)
        {
            return;
        }

        if (!currentTurnPlayer.PlayerRef.Equals(playerRef))
        {

            return;
        }

        if (!config.AllowCharacterSelectables.Contains(unitBanId))
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
        if (currentTurnPlayer == null || players.Count == 0)
        {
            return;
        }
        if (players.All(p => p.DeployedUnitIds.Count >= CurrentTurnCount))
        {
            CurrentTurnCount++;
        }
        currentTurnPlayer.ResetState();
        playerTurnIndex = (playerTurnIndex + 1) % players.Count;
        ProcessPlayersTurn();
    }

    public void BroadcastBanPickInfo()
    {
        bool shouldStartTurn = currentTurnPlayer == null;
        if (shouldStartTurn)
        {
            playerTurnIndex = UnityEngine.Random.Range(0, players.Count);
        }
        RoomSystem.TryGetRoomById(RoomId, out var room);

        currentMap = Master.Instance.LoadMap(room.MapIndexSelected);

        foreach (BattlePlayer battlePlayer in players)
        {
            Client battleClient = ClientManager.TryGetClient(battlePlayer.PlayerRef);
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
                Players = players.Select(p => new BattlePlayerInfo
                {
                    Name = p.Name,
                    DeployedUnitIds = p.DeployedUnitIds.ToList(),
                    BannedUnitIds = p.BannedUnitIds.ToList()
                }).ToArray()
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
        var playerTurn = players[playerTurnIndex];
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
            Service.SendBanPickTurnCountDown(currentSecond),
            players.Select(p => p.PlayerRef).ToArray());
    }

    #endregion


    #region Deployment State

    private void UpdateDeploymentState(float deltaTime)
    {
    }

    private void StartDeploymentPhase()
    {
        State = BattleState.Deployment;
        RoomSystem.TryGetRoomById(RoomId, out Room room);
        foreach (BattlePlayer battlePlayer in players)
        {
            Client battleClient = ClientManager.TryGetClient(battlePlayer.PlayerRef);
            if (battleClient == null)
            {
                continue;
            }
            ServerNetwork.Instance.SendToClient(battleClient, Service.LoadDeploymentPhase(new DeploymentPhaseInfo
            {
                DeployedUnitIds = battlePlayer.DeployedUnitIds.ToList(),
                tiles = currentMap.Tiles
            }));
        }
    }

    public void SetUnitPlaced(Client client, PlaceUnit placeUnit)
    {
        var battlePlayer = players.FirstOrDefault(x => x.PlayerRef == client.PlayerRef);
        if (battlePlayer == null) return;

        if (placeUnit.IndexSelected < 0 || placeUnit.IndexSelected >= battlePlayer.DeployedUnitIds.Count)
        {
            return;
        }

        int unitId = battlePlayer.DeployedUnitIds.ElementAt(placeUnit.IndexSelected);
        Unit unit = battlePlayer.UnitCombats.FirstOrDefault(x => x.Data.Id == unitId);

        if (!currentMap.IsValidSpawnPointPosition(battlePlayer.IsLeftSide, placeUnit.PlacedPosition))
        {
            if (unit != null)
            {
                battlePlayer.UnitCombats.Remove(unit);
                ServerNetwork.Instance.SendToClients(Service.RemoveUnit(unitId, battlePlayer.PlayerRef.PlayerId),
                players.Select(x => x.PlayerRef).ToArray());
            }
            return;
        }


        CharacterDataJsonData charData = unit?.Data;

        if (unit == null)
        {
            charData = Master.Instance.TacticalSOExportData.Characters.FirstOrDefault(x => x.Id == unitId);

            if (charData == null) return;

            unit = new Unit(charData, placeUnit.PlacedPosition);
            battlePlayer.AddUnit(unit);
        }
        else
        {
            unit.CurrentGridPosition = placeUnit.PlacedPosition;
        }

        ServerNetwork.Instance.SendToClients(Service.PlaceUnitResult(
            charData.Id, placeUnit.IndexSelected, placeUnit.PlacedPosition, battlePlayer.PlayerRef.PlayerId),
            players.Select(x => x.PlayerRef).ToArray());
    }
    #endregion
}

