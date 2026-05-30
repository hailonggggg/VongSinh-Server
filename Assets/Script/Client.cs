using Fusion;
using System;
using System.Collections.Generic;

public class Client : IEquatable<Client>
{
    public UserApiUserData User;
    public PlayerRef PlayerRef;
    public List<UserItem> UserItems;
    public HashSet<int> OwnedCharacterIds = new();
    public string Name => User != null ? $"{User.FirstName} {User.LastName}" : $"Player {PlayerRef.PlayerId}";
    public int CurrentRoomId = -1;
    public int CurrentBattleId = -1;
    public string Token;
    public string Password;

    public Queue<Action> PendingPacket = new();

    public Client(NetworkRunner networkRunner, PlayerRef playerRef)
    {
        PlayerRef = playerRef;
    }

    public bool Equals(Client other)
    {
        return PlayerRef == other.PlayerRef;
    }

    public void ResetCurrentBattleAndRoom()
    {
        CurrentBattleId = -1;
        CurrentRoomId = -1;
    }
}
