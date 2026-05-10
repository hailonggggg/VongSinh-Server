using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Script.System
{
    public class BattleSystem : BaseSystem
    {
        private static int nextBattleId = 1;
        private readonly static ConcurrentDictionary<int, Battle> battles = new();

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
                    HandleUnitPlaced(client, payload);
                    break;
                case Command.UnitDeploySelectedSkill:
                    HandleUnitDeploySelectedSkill(client, payload);
                    break;
                case Command.CompleteSetupDeployment:
                    HandleCompleteSetupDeployment(client, payload);
                    break;
                case Command.UnitMove:
                    HandleUnitMove(client, payload);
                    break;
            }
        }

        private void HandleUnitMove(Client client, string payload)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }
            UnitMoveRequest unitMoveRequest = JsonConvert.DeserializeObject<UnitMoveRequest>(payload);
            battle.HandleUnitMove(client, unitMoveRequest.UnitId, unitMoveRequest.TargetCell);
        }


        private void HandleCompleteSetupDeployment(Client client, string payload)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }
            if (!battle.TryMarkSetupDeploymentComplete(client))
            {
                return;
            }
            battle.StartCombatPhase();
        }


        private void HandleUnitDeploySelectedSkill(Client client, string payload)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }

            UnitDeploySelectedSkillRequest request = JsonUtility.FromJson<UnitDeploySelectedSkillRequest>(payload);
            if (request == null)
            {
                return;
            }

            if (!battle.TryHandleUnitDeploySelectedSkill(client.PlayerRef, request))
            {
                ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Không thể gắn thêm kỹ năng vào nhóm này. Mỗi unit chỉ được tối đa 2 basic attack, 2 kỹ năng và 2 passive. Hãy xóa 1 kỹ năng trước."));
            }
        }

        private void HandleUnitPlaced(Client client, string payload)
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
                ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Khong the trien khai don vi nay. Hay chac chan rang ban da chon dung don vi va chua vuot qua gioi han trien khai."));
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
                    Service.ShowNotification("Khong du dieu kien de bat dau tran dau. Hay chac chan rang tat ca nguoi choi da san sang, co it nhat 2 nguoi choi va khong co ai dang trong tran dau khac."));
                return;
            }

            int battleId = nextBattleId++;
            List<BattlePlayer> battlePlayers = room.Players
                .Select((player, index) => new BattlePlayer(player.Client, player.Name, index == 0))
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
