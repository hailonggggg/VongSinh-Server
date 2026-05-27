using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Script.System;
using UnityEngine;

public class RoomSystem : BaseSystem
{
    private static readonly Dictionary<int, Room> rooms = new();
    private static int nextRoomId = 1;

    private static readonly Dictionary<string, PendingMatch> pendingMatches = new();

    public class PendingMatch
    {
        public Client Player1 { get; set; }
        public Client Player2 { get; set; }
        public bool Player1Ready { get; set; }
        public bool Player2Ready { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static bool TryGetRoomById(int roomId, out Room room)
    {
        return rooms.TryGetValue(roomId, out room);
    }

    public static bool TryGetRoomByName(string roomName, out Room room)
    {
        room = rooms.Values.FirstOrDefault(x => x.Name == roomName);
        return room != null;
    }

    public override void HandlePackage(Client client, Command messageType, string payload)
    {
        base.HandlePackage(client, messageType, payload);
        switch (messageType)
        {
            case Command.RequestRoomList:
                ServerNetwork.Instance.SendToClient(client, Service.SendRoomList(GetAllRooms()));
                break;
            case Command.CreateRoom:
                CreateRoom(client, JsonUtility.FromJson<CreateRoomRequest>(payload));
                break;
            case Command.KickPlayer:
                KickPlayerOutOfRoom(client, JsonUtility.FromJson<KickPlayerRequest>(payload));
                break;
            case Command.LeaveRoom:
                LeaveRoom(client);
                break;
            case Command.JoinRoom:
                JoinRoom(client, JsonUtility.FromJson<JoinRoomRequest>(payload));
                break;
            case Command.PlayerReady:
                SetPlayerReadyStatus(client);
                break;
            case Command.MapIndexSelected:
                HandleMapIndexSelected(client, payload);
                break;
            case Command.RequestRandomMatch:
                HandleRequestRandomMatch(client);
                break;
            case Command.CancelRandomMatch:
                HandleCancelRandomMatch(client);
                break;
            case Command.ConfirmMatch:
                HandleConfirmMatch(client, payload);
                break;
            default:
                break;
        }
    }

    private void HandleMapIndexSelected(Client client, string payload)
    {
        if (client.CurrentRoomId <= 0 || !TryGetRoomById(client.CurrentRoomId, out Room room))
        {
            return;
        }
        BattlePlayerInfo playerInfo = room.Players.FirstOrDefault(x => x.Client == client);
        if (!playerInfo.IsHost)
        {
            return;
        }
        room.MapIndexSelected = JsonUtility.FromJson<SelectedMapIndexRequest>(payload).Index;
        ServerNetwork.Instance.SendToClients(Service.UpdateRoom(room), room.Players.Select(p => p.Client).ToArray());
    }

    private void KickPlayerOutOfRoom(Client client, KickPlayerRequest kickPlayerRequest)
    {
        if (!TryGetRoomByName(kickPlayerRequest.RoomName, out Room room))
        {
            return;
        }

        BattlePlayerInfo hostPlayer = room.Players.Find(p => p.IsHost && p.Client == client);
        if (hostPlayer == null)
        {
            return;
        }

        int playerKickedIndex = room.Players.FindIndex(p => p.Name == kickPlayerRequest.PlayerName);
        if (playerKickedIndex < 0)
        {
            return;
        }

        BattlePlayerInfo playerKicked = room.Players[playerKickedIndex];
        room.Players.RemoveAt(playerKickedIndex);
        playerKicked.Client.CurrentRoomId = -1;
        playerKicked.Client.CurrentBattleId = -1;

        ServerNetwork.Instance.SendToClients(Service.UpdateRoom(room), room.Players.Select(x => x.Client.PlayerRef).ToArray());
        ServerNetwork.Instance.SendToClient(playerKicked.Client, Service.LoadLobbyScene());
    }

    public void CreateRoom(Client client, CreateRoomRequest createRoomRequest)
    {
        if (TryGetRoomByName(createRoomRequest.RoomName, out _) || client.CurrentRoomId > 0)
        {
            Debug.LogError("Room already exists!");
            return;
        }

        int roomId = nextRoomId++;
        var hostPlayer = new BattlePlayerInfo
        {
            PlayerId = client.PlayerRef.PlayerId,
            Name = client.User.LastName,
            IsHost = true,
            AvatarUrl = client.User.AvatarUrl,
            Client = client,
        };

        rooms[roomId] = new Room
        {
            RoomId = roomId,
            Name = createRoomRequest.RoomName,
            Players = new List<BattlePlayerInfo>(createRoomRequest.MaxPlayers) { hostPlayer },
            MaxPlayers = createRoomRequest.MaxPlayers
        };

        client.CurrentRoomId = roomId;
        ServerNetwork.Instance.SendToClient(client, Service.UpdateRoom(rooms[roomId]));
        ServerNetwork.Instance.SendToClient(client, Service.LoadRoomScene());
        ServerNetwork.Instance.BroadcastToAllClientsExcept(client, Service.SendRoomList(GetAllRooms()));
    }

    public void JoinRoom(Client client, JoinRoomRequest joinRequest)
    {
        if (!TryGetRoomById(joinRequest.RoomId, out Room room))
        {
            Debug.LogError($"[ROOM] Room {joinRequest.RoomId} does not exist.");
            return;
        }

        var roomPlayer = new BattlePlayerInfo
        {
            PlayerId = client.PlayerRef.PlayerId,
            Name = client.User.LastName,
            IsHost = false,
            IsReady = false,
            AvatarUrl = client.User.AvatarUrl,
            Client = client
        };

        room.Players.Add(roomPlayer);
        client.CurrentRoomId = room.RoomId;
        Debug.Log($"[ROOM] Client {client.User.LastName} joined room {room.Name}");
        ServerNetwork.Instance.SendToClients(Service.UpdateRoom(room), room.Players.Select(x => x.Client).ToArray());
        ServerNetwork.Instance.SendToClient(client, Service.LoadRoomScene());
        ServerNetwork.Instance.BroadcastToAllClientsExcept(client, Service.UpdateRoomInfo(new RoomInfo
        {
            Name = room.Name,
            PlayerCount = room.Players.Count,
            MaxPlayers = room.MaxPlayers
        }));
    }

    public IEnumerable<Room> GetAllRooms()
    {
        return rooms.Values;
    }

    public void RemoveRoom(Client client)
    {
        if (!TryGetRoomById(client.CurrentRoomId, out Room room))
        {
            // Debug.Log("[ROOM] Room not found.");
            return;
        }

        Client clientNotHost = room.Players.FirstOrDefault(x => !x.IsHost)?.Client;
        for (int i = 0; i < room.Players.Count; i++)
        {
            room.Players[i].IsReady = false;
            room.Players[i].IsHost = false;
            room.Players[i].Client.CurrentRoomId = -1;
            room.Players[i].Client.CurrentBattleId = -1;
        }

        rooms.Remove(room.RoomId);
        ServerNetwork.Instance.SendToClient(clientNotHost, Service.LoadLobbyScene());
        ServerNetwork.Instance.BroadcastToAllClientsExcept(client, Service.SendRoomList(GetAllRooms()));
    }

    public void LeaveRoom(Client client)
    {
        if (client == null || client.CurrentRoomId < 0)
        {
            return;
        }

        if (!TryGetRoomById(client.CurrentRoomId, out Room room))
        {
            return;
        }

        BattlePlayerInfo roomPlayer = room.Players.Find(p => p.Client == client);
        if (roomPlayer == null)
        {
            return;
        }

        if (roomPlayer.IsHost)
        {
            RemoveRoom(client);
            return;
        }

        if (RemoveClientFromRoom(client, room))
        {
            ServerNetwork.Instance.SendToClient(room.Players.FirstOrDefault(x => x.Client != client).Client, Service.UpdateRoom(room));
        }
    }

    private bool RemoveClientFromRoom(Client client, Room room)
    {
        int playerIndex = room.Players.FindIndex(p => p.Client == client);
        if (playerIndex == -1)
        {
            return false;
        }

        room.Players.RemoveAt(playerIndex);
        client.CurrentRoomId = -1;
        client.CurrentBattleId = -1;
        return true;
    }

    public void SetPlayerReadyStatus(Client client)
    {
        if (!TryGetRoomById(client.CurrentRoomId, out Room room))
        {
            return;
        }

        BattlePlayerInfo me = room.Players.FirstOrDefault(p => p.Client == client);
        if (me == null)
        {
            Debug.Log("RoomPlayer not found!");
            return;
        }

        me.IsReady = !me.IsReady;
        if (room.Players.All(x => x.IsReady))
        {
            BattleSystem.CreateBattle(client, room.Name);
        }
        else
        {
            ServerNetwork.Instance.SendToClients(Service.UpdateRoom(room), room.Players.Select(x => x.Client.PlayerRef).ToArray());
        }
    }

    private void HandleRequestRandomMatch(Client client)
    {
        if (client.CurrentRoomId > 0)
        {
            Debug.LogError("Player is already in a room!");
            return;
        }
        MatchmakingQueue.AddPlayer(client);
        ServerNetwork.Instance.SendToClient(client, Service.SendMatchmakingResponse(true, "Đang tìm trận"));
    }

    public static void HandleCancelRandomMatch(Client client)
    {
        MatchmakingQueue.RemovePlayer(client);

        var matchKey = pendingMatches.Keys.FirstOrDefault(k =>
            pendingMatches[k].Player1 == client || pendingMatches[k].Player2 == client);
        if (matchKey != null)
        {
            pendingMatches.Remove(matchKey);
        }

        ServerNetwork.Instance.SendToClient(client, Service.SendMatchmakingResponse(false, "Tìm trận"));
    }

    private void HandleConfirmMatch(Client client, string payload)
    {
        var request = JsonUtility.FromJson<ConfirmMatchRequest>(payload);
        bool confirmed = request.Confirmed;

        var match = pendingMatches.Values.FirstOrDefault(m =>
            m.Player1 == client || m.Player2 == client);

        if (match == null) return;

        if (match.Player1 == client)
        {
            match.Player1Ready = confirmed;
        }
        else if (match.Player2 == client)
        {
            match.Player2Ready = confirmed;
        }

        if (match.Player1Ready && match.Player2Ready)
        {
            StartBattleFromMatch(match);
        }
        else if (!confirmed)
        {
            var otherClient = match.Player1 == client ? match.Player2 : match.Player1;
            pendingMatches.Remove(pendingMatches.FirstOrDefault(k => k.Value == match).Key);
            MatchmakingQueue.AddPlayer(otherClient);
            HandleCancelRandomMatch(client);
        }
    }

    public static void TryCreatePendingMatch()
    {
        var pair = MatchmakingQueue.GetPair();
        if (pair == null) return;

        string matchKey = Guid.NewGuid().ToString();

        var pending = new PendingMatch
        {
            Player1 = pair[0].Client,
            Player2 = pair[1].Client,
            CreatedAt = DateTime.UtcNow
        };

        pendingMatches[matchKey] = pending;
        MatchmakingQueue.RemovePair(pair);

        var response = new MatchFoundResponse
        {
            MatchId = matchKey,
            PlayerName = pending.Player2.User.LastName
        };
        ServerNetwork.Instance.SendToClient(pending.Player1, Service.SendMatchFound(response));

        response.PlayerName = pending.Player1.User.LastName;
        ServerNetwork.Instance.SendToClient(pending.Player2, Service.SendMatchFound(response));
    }

    public static void CheckPendingMatchTimeouts()
    {
        var now = DateTime.UtcNow;
        var timedOutMatches = pendingMatches.Where(kvp => (now - kvp.Value.CreatedAt).TotalSeconds >= 10).ToList();

        foreach (var entry in timedOutMatches)
        {
            var match = entry.Value;
            string matchKey = entry.Key;

            pendingMatches.Remove(matchKey);

            if (!match.Player1Ready)
            {
                MatchmakingQueue.RemovePlayer(match.Player1);
                ServerNetwork.Instance.SendToClient(match.Player1, Service.SendMatchmakingResponse(false, "Tìm trận"));
            }

            if (!match.Player2Ready)
            {
                MatchmakingQueue.RemovePlayer(match.Player2);
                ServerNetwork.Instance.SendToClient(match.Player2, Service.SendMatchmakingResponse(false, "Tìm trận"));
            }
        }
    }

    private void StartBattleFromMatch(PendingMatch match)
    {
        BattleSystem.CreateBattle(new List<Client> { match.Player1, match.Player2 });
        pendingMatches.Remove(pendingMatches.FirstOrDefault(k => k.Value == match).Key);
    }

    public static void SendMatchmakingUpdates()
    {
        foreach (var player in MatchmakingQueue.GetQueue())
        {
            var update = new MatchmakingUpdate
            {
                WaitTime = player.WaitTime,
                QueuePosition = MatchmakingQueue.GetQueue().IndexOf(player) + 1,
                TotalInQueue = MatchmakingQueue.GetQueue().Count
            };
            ServerNetwork.Instance.SendToClient(player.Client, Service.SendMatchmakingUpdate(update));
        }
    }
}
