using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Newtonsoft.Json;
using UnityEngine;

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
            case Command.RequestUploadAvatar:
                _ = HandleUploadAvatar(client, payload);
                break;
            case Command.RankList:
                _ = HandleRankList(client, payload);
                break;
        }
    }

    private async Task HandleRankList(Client client, string payload)
    {
        var request = JsonConvert.DeserializeObject<RankingLeaderBoardRequest>(payload);
        LeaderboardResponse response = await ApiService.GetRankingLeaderBoard(client, request);
        if (response == null)
            return;
        ServerNetwork.Instance.SendToClient(client, Service.RankingLeaderBoardResponse(response));
    }

    private async Task HandleUploadAvatar(Client client, string payload)
    {
        UploadAvatarRequest uploadAvatarRequest = JsonConvert.DeserializeObject<UploadAvatarRequest>(payload);
        byte[] imageByteArr = Convert.FromBase64String(uploadAvatarRequest.ImageBase64);
        string url = await ApiService.UpLoadImage(client, imageByteArr, uploadAvatarRequest.FileExtension);

        if (string.IsNullOrEmpty(url))
            return;

        client.User.AvatarUrl = url;
        ServerNetwork.Instance.SendToClient(client, Service.UpLoadAvatarResponse(true, "", url));
    }
}
