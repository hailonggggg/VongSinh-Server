using Fusion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
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

    public static byte[] SendSkinAndCharacterBundleResponse(
    SkinAndCharacterBundleResponse[] bundles)
    {
        string json = JsonConvert.SerializeObject(bundles);

        return ReliableMessage.Build(
            Command.SkinAndCharacterBundleResponse,
            json
        );
    }

    public static byte[] SendOrderResponse(OrderResponse order)
    {
        string json = JsonConvert.SerializeObject(order);
        return ReliableMessage.Build(Command.OrderResponse, json);
    }

    public static byte[] SendPlayerTurnToDeploy(int currentTurnCount, int playerId)
    {
        return ReliableMessage.Build(Command.PlayerTurnToDeploy, new PlayerTurnToDeploy
        {
            TurnCount = currentTurnCount,
            PlayerId = playerId
        });
    }

    public static byte[] SendTimeCountDown(float currentCountDown)
    {
        return ReliableMessage.Build(Command.TimeCountDown, new
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

    public static byte[] PlaceUnitResult(int id, int index, GridFacing facingDirection, Vector3Int placedPosition, int playerRefId)
    {
        return ReliableMessage.Build(Command.UnitPlaced, new UnitPlaced
        {
            UnitId = id,
            Index = index,
            FacingDirection = facingDirection,
            PlacedPosition = placedPosition,
            PlayerRefId = playerRefId
        });
    }

    public static byte[] RemoveUnit(int unitId, int playerRefId)
    {
        return ReliableMessage.Build(Command.RemoveUnit, new RemoveUnit
        {
            PlayerRefId = playerRefId,
            UnitId = unitId
        });
    }

    public static byte[] SendInventoryResponse(UserItemWithDetail[] items)
    {
        string json = JsonConvert.SerializeObject(items);
        return ReliableMessage.Build(Command.InventoryResponse, json);
    }

    public static byte[] SendGameData(GameDataResponse gameDataResponse)
    {
        string json = JsonConvert.SerializeObject(gameDataResponse);
        return ReliableMessage.Build(Command.GameData, json);
    }

    public static byte[] SendUnitDeploySelectedSkill(int playerId, int unitId, int skillId, int type, bool isChecked)
    {
        string json = JsonConvert.SerializeObject(new UnitDeploySelectedSkillResponse
        {
            PlayerId = playerId,
            UnitId = unitId,
            SkillId = skillId,
            Type = type,
            IsChecked = isChecked
        });
        return ReliableMessage.Build(Command.UnitDeploySelectedSkill, json);
    }

    public static byte[] StartCombatPhase()
    {
        return ReliableMessage.Build(Command.StartCombatPhase, new byte[0]);
    }

    public static byte[] UnitMove(int playerId, int id, List<Vector3Int> paths)
    {
        return ReliableMessage.Build(Command.UnitMove, new UnitMoveResponse
        {
            PlayerId = playerId,
            UnitId = id,
            Paths = paths
        });
    }

    public static byte[] CombatTurnInfo(int currentPlayerId)
    {
        return ReliableMessage.Build(Command.CombatTurnInfo, new CombatTurnInfo
        {
            PlayerId = currentPlayerId,
        });
    }

    public static byte[] PlayerResourceInfo(int apPoint, int yuanPressurePoint)
    {
        return ReliableMessage.Build(Command.PlayerResourceInfo, new PlayerResourceInfo
        {
            ApPoint = apPoint,
            YuanPressurePoint = yuanPressurePoint
        });
    }

    public static byte[] UseSkillResult(int playerId, int unitId, string AnimationTrigger, Vector3Int targetCell)
    {
        return ReliableMessage.Build(Command.UseSkill, new UseSkillResult
        {
            PlayerId = playerId,
            UnitId = unitId,
            AnimationTrigger = AnimationTrigger,
            TargetCell = targetCell
        });
    }

    public static byte[] ReceiveDamage(int playerId, int id, int damage, int currentHealth)
    {
        return ReliableMessage.Build(Command.ReceiveDamage, new ReceiveDamageResult
        {
            PlayerId = playerId,
            UnitId = id,
            DamageAmount = damage,
            RemainingHealth = currentHealth
        });
    }

    public static byte[] UnitInfoResult(int playerId, int id, int skillPoint, int currentHealth)
    {
        return ReliableMessage.Build(Command.UnitInfo, new UnitInfoResult
        {
            PlayerId = playerId,
            UnitId = id,
            SkillPoint = skillPoint,
            RemainingHealth = currentHealth
        });
    }

    public static byte[] UnitHealResult(int playerId, int id, int healAmount, int currentHealth)
    {
        return ReliableMessage.Build(Command.UnitHeal, new UnitHealResult
        {
            PlayerId = playerId,
            UnitId = id,
            HealAmount = healAmount,
            RemainingHealth = currentHealth
        });
    }

    public static byte[] UnitDeathResult(int playerId, int id)
    {
        return ReliableMessage.Build(Command.UnitDeath, new UnitDeathResult
        {
            PlayerId = playerId,
            UnitId = id,
        });
    }

    public static byte[] YuanPressureUpdate(int current)
    {
        return ReliableMessage.Build(Command.YuanPressureUpdate, new YuanPressureUpdate
        {
            Current = current
        });
    }
}
