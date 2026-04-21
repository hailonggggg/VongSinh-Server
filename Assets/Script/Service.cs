using Fusion;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class Service
{
    public static byte[] SendRoomList(IEnumerable<Room> rooms)
    {
        RoomList roomList = new RoomList
        {
            Rooms = rooms.Select(x => new RoomInfo
            {
                Name = x.Name,
                PlayerCount = x.Players.Count,
                MaxPlayers = x.MaxPlayers
            }).ToList()
        };
        string roomListJson = JsonUtility.ToJson(roomList);
        Debug.Log($"[ROOM] Broadcasting Room List: {roomListJson}");
        byte[] payload = System.Text.Encoding.UTF8.GetBytes(roomListJson);
        return ReliableMessage.Build(Command.RoomListResponse, payload);
    }

    public static byte[] SendLoginResponse(string playerName)
    {
        LoginResponse loginResponse = new LoginResponse
        {
            Success = true,
            PlayerName = playerName
        };
        return ReliableMessage.Build(Command.LoginResponse, loginResponse);
    }

    public static byte[] SendLoginFailedResponse(string message)
    {
        LoginResponse loginResponse = new LoginResponse
        {
            Success = false,
            Message = message
        };
        return ReliableMessage.Build(Command.LoginResponse, loginResponse);
    }

    public static byte[] ShowNotification(string message)
    {
        return ReliableMessage.Build(Command.ShowNotification, new NotificationMessage
        {
            Message = message
        });
    }

    private static byte[] GetData(object obj)
    {
        return Encoding.UTF8.GetBytes(JsonUtility.ToJson(obj));
    }

    public static byte[] UpdateRoomInfo(RoomInfo roomInfo)
    {
        // byte[] payload = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(roomInfo));
        return ReliableMessage.Build(Command.UpdateRoomInfo, roomInfo);
    }

    public static byte[] LoadLobbyScene()
    {
        return ReliableMessage.Build(Command.LoadLobbyScene, new byte[0]);
    }

    public static byte[] LoadRoomScene()
    {
        return ReliableMessage.Build(Command.LoadRoomScene, new byte[0]);
    }
    public static byte[] LoadBattleScene()
    {
        return ReliableMessage.Build(Command.LoadBattleScene, new byte[0]);
    }

    public static byte[] SendBanPickStartInfo(BattleBanPickInfo battleStartInfo)
    {
        return ReliableMessage.Build(Command.BattleBanPickInfo, battleStartInfo);
    }

    public static byte[] UpdateRoom(Room room)
    {
        return ReliableMessage.Build(Command.UpdateRoom, room);
    }

    public static byte[] SendRegisterResponse(bool success, string message)
    {
        RegisterResponse registerResponse = new RegisterResponse
        {
            Success = success,
            Message = message
        };
        return ReliableMessage.Build(Command.RegisterResponse, registerResponse);
    }
    public static byte[] SendAnnouncementResponse(AnnouncementResponse[] announcements)
    {
        string json = JsonConvert.SerializeObject(announcements);
        return ReliableMessage.Build(Command.AnnouncementResponse, json);
    }

    [Serializable]
    private class AnnouncementWrapper
    {
        public AnnouncementResponse[] Data;
    }

    public static byte[] SendGemBundleResponse(GemBundleResponse[] bundles)
    {
        string json = JsonConvert.SerializeObject(bundles);
        return ReliableMessage.Build(Command.GemBundleResponse, json);
    }

    public static byte[] SendOrderResponse(OrderResponse order)
    {
        string json = JsonConvert.SerializeObject(order);
        return ReliableMessage.Build(Command.OrderResponse, json);
    }
}
