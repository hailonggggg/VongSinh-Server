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

        public static IEnumerable<Battle> AllBattles => battles.Values;

        public void Tick(float deltaTime)
        {
            foreach (Battle battle in battles.Values)
            {
                battle.Tick(deltaTime);
            }
        }

        public override void HandlePackage(Client client, Command messageType, string payload)
        {
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
                case Command.ActionComplete:
                    HandleActionComplete(client);
                    break;
                case Command.UseSkill:
                    HandleUseSkill(client, payload);
                    break;
                case Command.OnFrameHit:
                    HandleOnFrameHit(client, payload);
                    break;
                case Command.EndTurn:
                    HandleEndTurn(client);
                    break;
            }
        }


        private void HandleEndTurn(Client client)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }
            battle.HandleEndTurn(client);
        }

        private void HandleOnFrameHit(Client client, string payload)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }
            battle.HandleOnFrameHit(client);
        }

        private void HandleUseSkill(Client client, string payload)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }

            UseSkillRequest request = JsonUtility.FromJson<UseSkillRequest>(payload);
            if (request == null)
            {
                return;
            }

            battle.HandleUseSkill(client, request);
        }

        private void HandleActionComplete(Client client)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }
            battle.HandleActionComplete(client);
        }

        private void HandleUnitMove(Client client, string payload)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }
            UnitMoveRequest unitMoveRequest = JsonConvert.DeserializeObject<UnitMoveRequest>(payload);
            battle.HandleUnitMove(client, unitMoveRequest.UnitId, unitMoveRequest.CurrentCell, unitMoveRequest.TargetCell);
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

            battle.HandleUnitIdBanned(client, unitBanId);
        }

        private void HandleUnitDeploySelected(Client client, string payload)
        {
            if (!TryGetBattle(client, out Battle battle))
            {
                return;
            }

            UnitDeployInfo unitDeployInfo = JsonUtility.FromJson<UnitDeployInfo>(payload);
            if (!battle.HandleUnitIdPicked(client, unitDeployInfo.UnitId))
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

            if (CheckAllPlayerIfAnyNotHaveUnlockedUnit(room.Players.Select(p => p.Client).ToList()))
            {
                ServerNetwork.Instance.SendToClient(
                    host,
                    Service.ShowNotification("Không thể bắt đầu trận đấu. Hãy chắc chắn rằng tất cả người chơi đã mở khóa ít nhất 1 đơn vị."));
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
            battle.OnBattleEnded += RemoveBattle;

            if (!battles.TryAdd(battleId, battle))
            {
                return;
            }

            foreach (BattlePlayerInfo info in room.Players)
            {
                info.Client.CurrentBattleId = battleId;
                info.IsReady = false;
                BattleSceneLoaded(info.Client);
            }
        }

        public static void CreateBattle(List<Client> clients)
        {
            if (clients.Any(x => x == null || x.CurrentRoomId > 0 || x.CurrentBattleId > 0))
            {
                return;
            }

            if (CheckAllPlayerIfAnyNotHaveUnlockedUnit(clients))
            {
                ServerNetwork.Instance.SendToClients(
                    Service.ShowNotification("Không thể bắt đầu trận đấu. Hãy chắc chắn rằng tất cả người chơi đã mở khóa ít nhất 1 đơn vị."),
                    clients.ToArray()
                );
                return;
            }
            int battleId = nextBattleId++;
            List<BattlePlayer> battlePlayers = clients
                .Select((player, index) => new BattlePlayer(player, player.Name, index == 0))
                .ToList();

            Battle battle = new(battleId, -1, battlePlayers, true);
            battle.OnBattleEnded += RemoveBattle;

            if (!battles.TryAdd(battleId, battle))
            {
                return;
            }

            foreach (Client client in clients)
            {
                client.CurrentBattleId = battleId;
                BattleSceneLoaded(client);
            }
        }

        private static bool CheckAllPlayerIfAnyNotHaveUnlockedUnit(List<Client> clients)
        {
            foreach (var client in clients)
            {
                if (client.OwnedCharacterIds == null || client.OwnedCharacterIds.Count == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryGetBattle(Client client, out Battle battle)
        {
            battle = null;
            return client != null && client.CurrentBattleId > 0 && battles.TryGetValue(client.CurrentBattleId, out battle);
        }

        public static bool TryGetBattleById(int battleId, out Battle battle)
        {
            return battles.TryGetValue(battleId, out battle);
        }

        private static void RemoveBattle(int battleId)
        {
            battles.TryRemove(battleId, out _);
        }
    }
}
