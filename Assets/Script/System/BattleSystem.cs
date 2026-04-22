using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Script.System
{
    public class BattleSystem : BaseSystem
    {
        private static int nextBattleId = 1;
        private readonly static ConcurrentDictionary<int, Battle> battles = new ConcurrentDictionary<int, Battle>();

        public void Tick(float deltaTime)
        {
            foreach (Battle battle in battles.Values)
            {
                battle.Tick(deltaTime);
            }
        }

        public override void HandlePackage(Client client, Command messageType, string payload)
        {
            base.HandlePackage(client, messageType, payload);
            switch (messageType)
            {
                case Command.UnitDeploySelected:
                    HandleUnitDeploySelected(client, payload);
                    break;
                case Command.BanPickSelected:
                    HandleBanPickSelected(client, payload);
                    break;
                case Command.PlaceUnit:
                    HandleunitPlaced(client, payload);
                    break;
            }
        }

        private void HandleunitPlaced(Client client, string payload)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }
            PlaceUnit unitPlaced = JsonUtility.FromJson<PlaceUnit>(payload);
            battle.SetUnitPlaced(client, unitPlaced);
        }

        private void HandleBanPickSelected(Client client, string payload)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }
            if (!int.TryParse(payload, out int unitBanId))
            {
                return;
            }

            battle.HandleBanPickSelected(client.PlayerRef, unitBanId);
        }

        private void HandleUnitDeploySelected(Client client, string payload)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }

            UnitDeployInfo unitDeployInfo = JsonUtility.FromJson<UnitDeployInfo>(payload);

            if (!battle.HandleUnitDeploySelected(client.PlayerRef, unitDeployInfo.UnitId))
            {
                ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Không thể triển khai đơn vị này. Hãy chắc chắn rằng bạn đã chọn đúng đơn vị và chưa vượt quá giới hạn triển khai."));
            }

        }

        private static void BattleSceneLoaded(Client client)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }

            if (!battle.TryMarkSceneLoaded(client.PlayerRef) || !battle.IsReadyToStart())
            {
                return;
            }

            battle.BroadcastBanPickInfo();
        }

        public static void CreateBattle(Client host, string roomName)
        {
            if (host == null || host.CurrentRoomId < 0)
            {
                return;
            }

            if (!RoomSystem.TryGetRoomById(host.CurrentRoomId, out Room room))
            {
                return;
            }

            if (!string.IsNullOrEmpty(roomName) && room.Name != roomName)
            {
                return;
            }

            bool isAllPlayerReady = room.Players.All(p => p.IsReady);
            bool hasPlayerAlreadyInBattle = room.Players.Any(p => p.Client.CurrentBattleId >= 0);
            if (!isAllPlayerReady || room.Players.Count < 2 || hasPlayerAlreadyInBattle)
            {
                ServerNetwork.Instance.SendToClient(
                    host,
                    Service.ShowNotification("Không đủ điều kiện để bắt đầu trận đấu. Hãy chắc chắn rằng tất cả người chơi đã sẵn sàng, có ít nhất 2 người chơi và không có ai đang trong trận đấu khác."));
                return;
            }

            int battleId = nextBattleId++;
            List<BattlePlayer> battlePlayers = room.Players
                .Select((p, index) => new BattlePlayer(p.Client.PlayerRef, p.Name, index == 0))
                .ToList();
            Battle battle = new(battleId, room.RoomId, battlePlayers);

            if (!battles.TryAdd(battleId, battle))
            {
                return;
            }

            foreach (RoomPlayer roomPlayer in room.Players)
            {
                roomPlayer.Client.CurrentBattleId = battleId;
                roomPlayer.Client.PendingPacket.Enqueue(() =>
                {
                    BattleSceneLoaded(roomPlayer.Client);
                });
                ServerNetwork.Instance.SendToClient(roomPlayer.Client, Service.LoadBattleScene());
            }
        }

        private static bool TryGetBattle(Client client, out Battle battle)
        {
            battle = null;
            return client != null && client.CurrentBattleId > 0 && battles.TryGetValue(client.CurrentBattleId, out battle);
        }

    }

}
