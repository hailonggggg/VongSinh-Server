using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(NetworkSceneManagerDefault))]
public class ServerNetwork : MonoBehaviour, INetworkRunnerCallbacks
{
    public static ServerNetwork Instance { get; private set; }
    public event Action<PlayerRef, ArraySegment<byte>> OnReceiveNetworkData;

    private NetworkRunner runner;

    async void Awake()
    {
        Instance = this;
        runner = await CreateNetworkRunner();
        DontDestroyOnLoad(gameObject);
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {

    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {

    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {

    }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

    }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {

    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {

    }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        ClientManager.AddClient(new Client(player));
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Master.Instance.ClearClientResource(ClientManager.TryGetClient(player));
        ClientManager.RemoveClient(player);
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {

    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        OnReceiveNetworkData?.Invoke(player, data);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {

    }
    public void OnSceneLoadStart(NetworkRunner runner)
    {

    }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {

    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {

    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }
    public void OnConnectedToServer(NetworkRunner runner)
    {

    }

    public void BroadcastToAllClients(byte[] packet)
    {
        foreach (var player in runner.ActivePlayers)
        {
            runner.SendReliableDataToPlayer(player, ReliableKey.FromInts(), packet);
        }
    }

    public void BroadcastToAllClientsExcept(PlayerRef @ref, byte[] packet)
    {
        foreach (var player in runner.ActivePlayers)
        {
            if (player == @ref)
                continue;

            runner.SendReliableDataToPlayer(player, ReliableKey.FromInts(), packet);
        }
    }
    public void BroadcastToAllClientsExcept(Client ignoreClient, byte[] packet)
    {
        foreach (var playerRef in runner.ActivePlayers)
        {
            if (playerRef == ignoreClient.PlayerRef)
                continue;

            SendToClient(playerRef, packet);
        }
    }

    public void SendToClient(PlayerRef player, byte[] packet)
    {
        runner.SendReliableDataToPlayer(player, ReliableKey.FromInts(), packet);
    }

    public void SendToClient(Client client, byte[] packet)
    {
        SendToClient(client.PlayerRef, packet);
    }

    public void SendToClient(Client client, params byte[][] packets)
    {
        foreach (var packet in packets)
        {
            SendToClient(client, packet);
        }
    }


    public void SendToClients(byte[] bytes, params PlayerRef[] players)
    {
        for (int i = 0; i < players.Length; i++)
        {
            SendToClient(players[i], bytes);
        }
    }
    
    public void SendToClients(byte[] bytes, params Client[] clients)
    {
        for (int i = 0; i < clients.Length; i++)
        {
            SendToClient(clients[i], bytes);
        }
    }

    public async Task<NetworkRunner> CreateNetworkRunner()
    {
        GameObject obj = new("NetworkRunner");
        NetworkRunner networkRunner = obj.AddComponent<NetworkRunner>();
        networkRunner.AddCallbacks(this);

        await networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Server,
            SessionName = "DedicatedServer",
            SceneManager = GetComponent<NetworkSceneManagerDefault>(),
            Scene = SceneRef.FromIndex(0)
        });

        return networkRunner;
    }
}
public enum Command : byte
{
    RoomListResponse,
    CreateRoom,
    JoinRoom,
    RequestLogin,
    RequestRegister,
    LoadLobbyScene,
    LoadRoomScene,
    RequestRoomList,
    RemoveRoom,
    UpdateRoomInfo,
    UpdateRoom,
    LeaveRoom,
    PlayerReady,
    CreateBattle,
    LoadBattleScene,
    KickPlayer,
    LoginResponse,
    RegisterRequest,
    RegisterResponse,
    ShowNotification,
    BattleBanPickInfo,
    BattleSceneLoaded,
    LoginWithFakeAccount,
    AnnouncementResponse,
    RequestAnnouncement,
    GemBundleResponse,
    RequestGemBundle,
    OrderResponse,
    RequestBuyBundle,
    RequestOrderStatus,
    UnitDeploySelected,
    BanPickSelected,
    BattlePlayerInfo,
    PlayerTurnToDeploy,
    BanPickTurnCountDown,
    MapIndexSelected,
    SceneLoadDone,
    Logout,
    LoadLoginScene,
    LoadDeploymentPhase
}
