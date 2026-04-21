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

    public virtual void HandlePackage(Client client, Command messageType, string payload)
    {
        switch (messageType)
        {
            case Command.SceneLoadDone:
                while (client.PendingPacket.TryDequeue(out Action action))
                {
                    action?.Invoke();
                }
                break;
        }
    }
}
