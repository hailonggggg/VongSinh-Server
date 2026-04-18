using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Script.System
{
    public class BattleSystem : BaseSystem
    {
        private static int nextBattleId = 1;
        private readonly ConcurrentDictionary<int, Battle> battles = new ConcurrentDictionary<int, Battle>();

        public override void HandlePackage(Client client, Command messageType, string payload)
        {
            switch (messageType)
            {
                case Command.CreateBattle:
                    string roomName = payload;
                    CreateBattle(client, roomName);
                    break;
                case Command.BattleSceneLoaded:
                    Debug.Log("BattleSceneLoaded");
                    BattleSceneLoaded(client);
                    break;
            }
        }

        private void BattleSceneLoaded(Client client)
        {
            if (client == null || client.CurrentBattleId < 0)
            {
                return;
            }

            if (!battles.TryGetValue(client.CurrentBattleId, out Battle battle))
            {
                return;
            }

            if (!battle.TryMarkSceneLoaded(client.PlayerRef) || !battle.IsReadyToStart())
            {
                return;
            }

            foreach (BattlePlayer battlePlayer in battle.Players)
            {
                Client battleClient = ClientManager.TryGetClient(battlePlayer.PlayerRef);
                if (battleClient == null)
                {
                    continue;
                }

                ServerNetwork.Instance.SendToClient(battleClient, Service.SendBanPickStartInfo(new BattleBanPickInfo
                {
                    AllowCharacterSelectables = battle.GetAllowCharacterSelectables()
                }));
            }
        }

        public void CreateBattle(Client host, string roomName)
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
                return;
            }

            int battleId = nextBattleId++;
            List<BattlePlayer> battlePlayers = room.Players
                .Select(p => new BattlePlayer(p.Client.PlayerRef, p.Name))
                .ToList();
            Battle battle = new(battleId, room.RoomId, battlePlayers);

            if (!battles.TryAdd(battleId, battle))
            {
                return;
            }

            foreach (RoomPlayer roomPlayer in room.Players)
            {
                roomPlayer.Client.CurrentBattleId = battleId;
                ServerNetwork.Instance.SendToClient(roomPlayer.Client, Service.LoadBattleScene());
            }
        }

    }
}
