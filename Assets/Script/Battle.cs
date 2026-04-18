using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;

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
    public BattleState State { get; private set; }
    public IReadOnlyList<BattlePlayer> Players => players;

    private readonly List<BattlePlayer> players;
    private BattleConfig config = new BattleConfig();

    public Battle(int battleId, int roomId, IEnumerable<BattlePlayer> players)
    {
        BattleId = battleId;
        RoomId = roomId;
        State = BattleState.WaitingForSceneLoad;
        this.players = new List<BattlePlayer>(players);
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

    public bool IsReadyToStart()
    {
        return State == BattleState.BanPick;
    }

    public int GetPlayerIndex(PlayerRef playerRef)
    {
        return players.FindIndex(x => x.PlayerRef == playerRef);
    }

    public int[] GetAllowCharacterSelectables()
    {
        return config.AllowCharacterSelectables;
    }

}
