using Fusion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
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
    public float disconnectCountDown;
    public float CurrentCountDown => currentCountDown;
    public int CurrentTurnCount;
    public BattleState State { get; private set; }
    public Client[] PlayerClients => playerClients;
    public Map CurrentMap => currentMap;
    public Dictionary<int, BattlePlayer> PlayersById => playersById;
    public BattleConfig Config => config;
    public event Action<int> OnBattleEnded;

    private readonly bool isRank;
    private readonly Dictionary<int, BattlePlayer> playersById;
    private readonly Client[] playerClients;
    private readonly BattleConfig config = Master.Instance.Config;

    private int playerTurnIndex = 0;
    private int lastBroadcastCountDownSecond = -1;
    private bool isSendDeploymentInfo;
    private float currentCountDown = 0f;
    private bool isEnd;
    private Map currentMap;
    private BattlePlayer currentTurnPlayer;

    public Battle(int battleId, int roomId, IEnumerable<BattlePlayer> players, bool isRank = false)
    {
        BattleId = battleId;
        RoomId = roomId;
        State = BattleState.WaitingForSceneLoad;
        playersById = players.ToDictionary(player => player.Client.PlayerRef.PlayerId);
        playerClients = players.Select(player => player.Client).ToArray();
        this.isRank = isRank;
    }

    public void Tick(float deltaTime)
    {
        if (isEnd) return;

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
            StartBanPickPhase();
        }

        return true;
    }

    #region BanPick State

    private void StartBanPickPhase()
    {
        State = BattleState.BanPick;
        ResetTurnState();
        CurrentTurnCount = 1;
    }

    public bool IsReadyToStart()
    {
        return State == BattleState.BanPick;
    }

    public bool HandleUnitIdPicked(Client client, int unitDeployId)
    {
        BattlePlayer player = GetPlayer(client.PlayerRef);
        BattlePlayer opponent = GetOpponent(client.PlayerRef);
        if (player == null || State != BattleState.BanPick || opponent == null)
        {
            return false;
        }

        if (currentTurnPlayer == null || !player.IsMyTurn(currentTurnPlayer.Client.PlayerRef))
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Lượt này không phải của bạn!"));
            return false;
        }

        if (!player.OwnerUnlockedUnitId.Contains(unitDeployId))
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Bạn chưa mở khóa nhân vật này!"));
            return false;
        }

        if (opponent.BannedUnitIds.Contains(unitDeployId))
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Nhân vật này đã bị đối phương cấm!"));
            return false;
        }

        if (!player.ApplyUnitIdPicked(unitDeployId))
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Đã thêm nhân vật này!"));
            return false;
        }
        ServerNetwork.Instance.SendToClients(Service.SendPlayerBanPickInfo(player), playerClients);
        if (playersById.Values.All(x => x.HasReachedDeployLimit(config.MaxUnitsPerPlayer)))
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

    public void HandleUnitIdBanned(Client client, int unitBanId)
    {
        BattlePlayer me = GetPlayer(client.PlayerRef);
        if (me == null || State != BattleState.BanPick)
        {
            return;
        }

        if (currentTurnPlayer == null || !me.IsMyTurn(currentTurnPlayer.Client.PlayerRef))
        {
            return;
        }

        BattlePlayer opponent = GetOpponent(client.PlayerRef);

        if (!opponent.OwnerUnlockedUnitId.Contains(unitBanId))
        {
            return;
        }

        if (!opponent.ApplyUnitIdBanned(unitBanId))
        {
            return;
        }
        ServerNetwork.Instance.SendToClients(Service.SendPlayerBanPickInfo(opponent), playerClients);
        ProcessPlayersTurn();
    }

    public void HandlePlayerTurnDone()
    {
        if (currentTurnPlayer == null || playersById.Values.Count == 0)
        {
            return;
        }
        currentTurnPlayer.ExecuteTurnDone();
        if (State == BattleState.BanPick)
        {
            if (currentTurnPlayer.PickedUnitIds.Count(x => x != -1) < config.MinUnitsPerPlayer)
            {
                State = BattleState.Finished;
                BattleEnd(GetOpponent(currentTurnPlayer.Client.PlayerRef), LoseReason.OpponentNotHaveAnyPickedUnit);
                return;
            }
            if (playersById.Values.All(p => p.PickedUnitIds.Count >= CurrentTurnCount) &&
               (!config.HasBanPhase || playersById.Values.All(p => p.BannedUnitIds.Count >= CurrentTurnCount * 2)))
            {
                CurrentTurnCount++;
            }
        }
        else if (State == BattleState.Combat)
        {
            CurrentTurnCount++;
        }
        playerTurnIndex = (playerTurnIndex + 1) % playersById.Values.Count;
        ProcessPlayersTurn();
    }

    private async void BattleEnd(BattlePlayer winner, LoseReason loseReason)
    {
        isEnd = true;
        BattlePlayer loser = GetOpponent(winner.Client.PlayerRef);

        foreach (var player in playerClients)
        {
            if (player == null) continue;

            BattlePlayer battlePlayer = GetPlayer(player.PlayerRef);
            battlePlayer.Client.CurrentBattleId = -1;
            bool isWinner = battlePlayer == winner;
            int currentRankPoint = player.User.RankPoint;
            int currentRank = 0;
            int newRankPoint = currentRankPoint;

            if (isRank)
            {
                if (isWinner)
                {
                    RankPointHandler.UpRankPoint(currentRankPoint, out currentRank, config.RankPointLimitToUpRank, out newRankPoint);
                }
                else
                {
                    RankPointHandler.DownRankPoint(currentRankPoint, out currentRank, config.RankPointLimitToUpRank, out newRankPoint);
                }
                await ApiService.SetRankPoint(battlePlayer.Client, newRankPoint);
            }
            ServerNetwork.Instance.SendToClient(player, Service.BattleResult(
                player.PlayerRef.PlayerId,
                isRank,
                isWinner,
                newRankPoint,
                currentRankPoint,
                config.RankPointLimitToUpRank
            ));

            if (RoomSystem.TryGetRoomById(player.CurrentRoomId, out Room room))
            {
                ServerNetwork.Instance.SendToClient(player, Service.UpdateRoom(room));
            }
            if (isWinner)
            {
                string msg = loseReason switch
                {
                    LoseReason.OpponentNotHaveAnyPickedUnit => "Bạn đã thắng, đối phương bị xử thua do không có nhân vật nào trong đội hình.",
                    LoseReason.OpponentDisconnected => "Bạn đã thắng, đối phương bị xử thua do rời trận",
                    LoseReason.OpponentNotDeployAnyUnit => "Bạn đã thắng, đối phương bị xử thua do không sắp đặt bất kì nhân vật nào",
                    _ => ""
                };
                if (!string.IsNullOrWhiteSpace(msg))
                    ServerNetwork.Instance.SendToClient(player, Service.ShowNotification(msg));
            }
            else
            {
                string msg = loseReason switch
                {
                    LoseReason.OpponentNotHaveAnyPickedUnit => "Bạn đã thua, đối phương thắng do bạn không có nhân vật nào trong đội hình.",
                    LoseReason.OpponentDisconnected => "Bạn đã thua do rời trận",
                    LoseReason.OpponentNotDeployAnyUnit => "Bạn đã thua do không sắp đặt bất kì nhân vật nào",
                    _ => ""
                };
                if (!string.IsNullOrWhiteSpace(msg))
                    ServerNetwork.Instance.SendToClient(player, Service.ShowNotification(msg));
            }
        }
        OnBattleEnded?.Invoke(BattleId);
    }

    public void BroadcastBanPickInfo()
    {
        bool shouldStartTurn = currentTurnPlayer == null;
        if (shouldStartTurn)
        {
            playerTurnIndex = UnityEngine.Random.Range(0, playersById.Values.Count);
        }

        RoomSystem.TryGetRoomById(RoomId, out Room room);
        int mapIndex = room != null ? room.MapIndexSelected : UnityEngine.Random.Range(0, Master.Instance.Config.AllowMapIds.Length);
        currentMap = Master.Instance.LoadMap(mapIndex);

        BattlePlayerInfo[] playerInfos = playersById.Values.Select(player => new BattlePlayerInfo
        {
            PlayerId = player.Client.PlayerRef.PlayerId,
            Name = player.Name,
            AvatarUrl = player.Client.User.AvatarUrl,
            PickedUnitIds = player.PickedUnitIds.ToList(),
            BannedUnitIds = player.BannedUnitIds.ToList(),
            OwnedUnits = player.OwnerUnlockedUnitId.ToList()
        })
        .ToArray();

        foreach (BattlePlayer battlePlayer in playersById.Values)
        {
            Client client = battlePlayer.Client;
            if (client == null)
            {
                continue;
            }
            ServerNetwork.Instance.SendToClient(client, Service.SendBanPickStartInfo(new BattleBanPickInfo
            {
                LeftSidePlayerId = battlePlayer.IsLeftSide ? battlePlayer.Client.PlayerRef.PlayerId : -1,
                HasBanPhase = config.HasBanPhase,
                MapIndexSelected = mapIndex,
                MaxUnitsPerPlayer = Mathf.Min(battlePlayer.OwnerUnlockedUnitId.Count, config.MaxUnitsPerPlayer),
                AllowCharacterSelectables = config.ListUnitIdHasData,
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
        ResetTurnState();
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
                DeployedUnitIds = battlePlayer.PickedUnitIds.ToList(),
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

    private BattlePlayer GetOpponent(PlayerRef playerRef)
    {
        return playersById.Values.FirstOrDefault(pl => pl.Client.PlayerRef != playerRef);
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

        if (player.UnitCombats.Count == 0)
        {
            BattleEnd(GetOpponent(client.PlayerRef), LoseReason.OpponentNotDeployAnyUnit);
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
        CurrentTurnCount = 0;
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
        currentTurnPlayer.HandleOnFrameHit(this);

        CheckTeamElimination();

        if (currentTurnPlayer.ApSystem.IsEmpty)
        {
            HandleActionComplete(client);
        }
    }

    private void CheckTeamElimination()
    {
        foreach (var player in playersById.Values)
        {
            if (player.IsTeamEliminated)
            {
                BattlePlayer opponent = GetOpponent(player.Client.PlayerRef);
                BattleEnd(opponent, LoseReason.OpponentEliminated);
                return;
            }
        }
    }


    public BattleContext CreateBattleContext(BattlePlayer player, Unit unit)
    {
        var allies = new List<Unit>(player.UnitCombats.Count - 1);
        foreach (var u in player.UnitCombats.Values)
        {
            if (u != unit) allies.Add(u);
        }

        var enemies = new List<Unit>();
        foreach (var p in playersById.Values)
        {
            if (p != player) enemies.AddRange(p.UnitCombats.Values);
        }

        return new BattleContext
        {
            Map = currentMap,
            ActionPointSystem = player.ApSystem,
            YuanPressureSystem = player.YuanPressureSystem,
            Allies = allies,
            Enemies = enemies,
        };
    }

    public void HandleEndTurn(Client client)
    {
        BattlePlayer player = GetPlayer(client.PlayerRef);
        if (player == null)
        {
            return;
        }

        if (currentTurnPlayer == null)
        {
            return;
        }
        if (currentTurnPlayer.Client.PlayerRef.PlayerId != client.PlayerRef.PlayerId)
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Không phải lượt của bạn nhưng bạn đang cố kết thúc lượt ?"));
            return;
        }
        HandlePlayerTurnDone();
    }

    public void HandleLeaveBattle(Client client)
    {
        BattleEnd(GetOpponent(client.PlayerRef), LoseReason.OpponentDisconnected);
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

public enum LoseReason
{
    OpponentNotHaveAnyPickedUnit,
    OpponentDisconnected,
    OpponentEliminated,
    OpponentNotDeployAnyUnit
}