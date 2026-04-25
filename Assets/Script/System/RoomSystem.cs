using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Script.System;
using UnityEngine;

public class RoomSystem : BaseSystem
{
    private static readonly Dictionary<int, Room> rooms = new Dictionary<int, Room>();
    private static int nextRoomId = 1;

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
                SetPlayerReadyStatus(client, JsonUtility.FromJson<PlayerReadyRequest>(payload));
                break;
            case Command.MapIndexSelected:
                HandleMapIndexSelected(client, payload);
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
        room.MapIndexSelected = JsonUtility.FromJson<SelectedMapIndexRequest>(payload).Index;
        ServerNetwork.Instance.SendToClients(Service.UpdateRoom(room), room.Players.Select(p => p.Client).ToArray());
    }

    private void KickPlayerOutOfRoom(Client client, KickPlayerRequest kickPlayerRequest)
    {
        if (!TryGetRoomByName(kickPlayerRequest.RoomName, out Room room))
        {
            return;
        }

        RoomPlayer hostPlayer = room.Players.Find(p => p.IsHost && p.Client == client);
        if (hostPlayer == null)
        {
            return;
        }

        int playerKickedIndex = room.Players.FindIndex(p => p.Name == kickPlayerRequest.PlayerName);
        if (playerKickedIndex < 0)
        {
            return;
        }

        RoomPlayer playerKicked = room.Players[playerKickedIndex];
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
        var hostPlayer = new RoomPlayer
        {
            Name = client.User.LastName,
            IsHost = true,
            Client = client,
        };

        rooms[roomId] = new Room
        {
            RoomId = roomId,
            Name = createRoomRequest.RoomName,
            Players = new List<RoomPlayer>(createRoomRequest.MaxPlayers) { hostPlayer },
            MaxPlayers = createRoomRequest.MaxPlayers
        };

        client.CurrentRoomId = roomId;
        client.PendingPacket.Enqueue(() =>
        {
            ServerNetwork.Instance.SendToClient(client, Service.UpdateRoom(rooms[roomId]));
        });
        ServerNetwork.Instance.SendToClient(client, Service.LoadRoomScene());
        ServerNetwork.Instance.BroadcastToAllClientsExcept(client, Service.SendRoomList(GetAllRooms()));
    }

    public void JoinRoom(Client client, JoinRoomRequest joinRequest)
    {
        if (!TryGetRoomByName(joinRequest.RoomName, out Room room))
        {
            Debug.LogError($"[ROOM] Room {joinRequest.RoomName} does not exist.");
            return;
        }

        var roomPlayer = new RoomPlayer
        {
            Name = client.User.LastName,
            IsHost = false,
            IsReady = false,
            Client = client
        };

        room.Players.Add(roomPlayer);
        client.CurrentRoomId = room.RoomId;
        Debug.Log($"[ROOM] Client {client.User.LastName} joined room {joinRequest.RoomName}");
        ServerNetwork.Instance.SendToClient(client, Service.LoadRoomScene());
        ServerNetwork.Instance.SendToClients(Service.UpdateRoom(room), room.Players.Select(x => x.Client.PlayerRef).ToArray());
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

    public void RemoveRoom(Client client, string roomName)
    {
        if (!TryGetRoomByName(roomName, out Room room))
        {
            Debug.Log("[ROOM] Room not found.");
            return;
        }

        Client[] clients = new Client[room.Players.Count];
        for (int i = 0; i < clients.Length; i++)
        {
            room.Players[i].IsReady = false;
            room.Players[i].IsHost = false;
            room.Players[i].Client.CurrentRoomId = -1;
            room.Players[i].Client.CurrentBattleId = -1;
            clients[i] = room.Players[i].Client;
        }

        rooms.Remove(room.RoomId);
        Debug.Log($"[ROOM] Room {roomName} removed by player {client.User.LastName}");
        ServerNetwork.Instance.SendToClients(Service.LoadLobbyScene(), clients);
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

        RoomPlayer roomPlayer = room.Players.Find(p => p.Client == client);
        if (roomPlayer == null)
        {
            return;
        }

        if (roomPlayer.IsHost)
        {
            RemoveRoom(client, room.Name);
            return;
        }

        if (RemoveClientFromRoom(client, room))
        {
            ServerNetwork.Instance.SendToClients(Service.UpdateRoom(room), room.Players.Select(x => x.Client.PlayerRef).ToArray());
            ServerNetwork.Instance.SendToClient(client, Service.LoadLobbyScene());
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

    public void SetPlayerReadyStatus(Client client, PlayerReadyRequest request)
    {
        if (!TryGetRoomById(client.CurrentRoomId, out Room room))
        {
            return;
        }

        RoomPlayer roomPlayer = room.Players.FirstOrDefault(p => p.Name == request.PlayerName);
        if (roomPlayer == null)
        {
            Debug.Log("RoomPlayer not found!");
            return;
        }

        roomPlayer.IsReady = !roomPlayer.IsReady;
        if (room.Players.All(x => x.IsReady))
        {
            BattleSystem.CreateBattle(client, room.Name);
        }
        else
        {
            ServerNetwork.Instance.SendToClients(Service.UpdateRoom(room), room.Players.Select(x => x.Client.PlayerRef).ToArray());
        }
    }
}
