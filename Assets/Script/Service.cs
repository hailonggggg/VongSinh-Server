using Fusion;
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

    public static byte[] SendLoginResponse(string playerName, string avatarUrl)
    {
        LoginResponse loginResponse = new LoginResponse
        {
            Success = true,
            PlayerName = playerName,
            PlayerAvatarUrl = avatarUrl
        };
        Debug.Log(avatarUrl);
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

    public static byte[] SendPlayerTurnToDeploy(int currentTurnCount, string name)
    {
        return ReliableMessage.Build(Command.PlayerTurnToDeploy, new PlayerTurnToDeploy
        {
            TurnCount = currentTurnCount,
            Name = name
        });
    }

    public static byte[] SendBanPickTurnCountDown(float currentCountDown)
    {
        return ReliableMessage.Build(Command.BanPickTurnCountDown, new BanPickTurnCountDown
        {
            TimeLeft = currentCountDown
        });
    }

    public static byte[] SendBattlePlayerInfo(BattlePlayer player)
    {
        return ReliableMessage.Build(Command.BattlePlayerInfo, new BattlePlayerInfo
        {
            Name = player.Name,
            DeployedUnitIds = player.DeployedUnitIds.ToList(),
            BannedUnitIds = player.BannedUnitIds.ToList()
        });
    }

    public static byte[] LoadLoginScene()
    {
        return ReliableMessage.Build(Command.LoadLoginScene, new byte[0]);
    }

    public static byte[] LoadDeploymentPhase(DeploymentPhaseInfo deploymentPhaseInfo)
    {
        return ReliableMessage.Build(Command.LoadDeploymentPhase, deploymentPhaseInfo);
    }
}
