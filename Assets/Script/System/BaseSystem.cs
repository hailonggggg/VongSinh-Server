using System;
using System.Text;
using Fusion;

public abstract class BaseSystem
{
    public BaseSystem()
    {
        StartListenerForNetworkData();
    }
    public void StartListenerForNetworkData()
    {
        ServerNetwork.Instance.OnReceiveNetworkData += HandleReceiveNetworkData;
    }
    public void HandleReceiveNetworkData(PlayerRef playerRef, ArraySegment<byte> data)
    {
        Client client = ClientManager.TryGetClient(playerRef);
        (Command type, string payload) = ReliableMessage.Parse(data);
        HandlePackage(client, type, payload);
    }

    public abstract void HandlePackage(Client client, Command messageType, string payload);
}
