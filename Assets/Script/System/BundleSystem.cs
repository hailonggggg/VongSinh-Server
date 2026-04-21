using System;
using System.Threading.Tasks;
using UnityEngine;

public class BundleSystem : BaseSystem
{
    public override void HandlePackage(Client client, Command messageType, string payload)
    {
        switch (messageType)
        {
            case Command.RequestGemBundle:
                _ = HandleRequestGemBundle(client);
                break;
        }
    }

    private async Task HandleRequestGemBundle(Client client)
    {
        try
        {
            if (client == null)
            {
                Debug.LogWarning("[BUNDLE] Client is null");
                return;
            }

            if (string.IsNullOrEmpty(client.Token))
            {
                Debug.LogWarning("[BUNDLE] Missing token");

                ServerNetwork.Instance.SendToClient(
                    client,
                    Service.ShowNotification("Bạn chưa đăng nhập.")
                );
                return;
            }

            Debug.Log("[BUNDLE] Fetching gem bundles...");

            GemBundleResponse[] bundles =
                await ApiService.GetAllGemBundle(client);

            if (bundles == null)
            {
                Debug.LogWarning("[BUNDLE] API returned null");

                ServerNetwork.Instance.SendToClient(
                    client,
                    Service.ShowNotification("Không thể tải shop.")
                );
                return;
            }

            Debug.Log($"[BUNDLE] Sending {bundles.Length} bundles");

            ServerNetwork.Instance.SendToClient(
                client,
                Service.SendGemBundleResponse(bundles)
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[BUNDLE] Error: {e}");

            ServerNetwork.Instance.SendToClient(
                client,
                Service.ShowNotification("Lỗi tải shop.")
            );
        }
    }
}