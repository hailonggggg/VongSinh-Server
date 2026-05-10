using Newtonsoft.Json;
using System;
using System.Text;
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

            case Command.RequestSkinAndCharacterBundle:
                _ = HandleRequestSkinBundle(client);
                break;

            case Command.RequestPurchaseSkinBundle:
                _ = HandlePurchaseSkinBundle(client, payload);
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
                    Service.ShowNotification("Không tải được shop pha lê.")
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

    private async Task HandleRequestSkinBundle(Client client)
    {
        try
        {
            if (client == null)
            {
                Debug.LogWarning("[SKIN BUNDLE] Client null");
                return;
            }

            SkinAndCharacterBundleResponse[] bundles =
                await ApiService.GetAllSkinAndCharacterBundle(client);

            if (bundles == null)
            {
                ServerNetwork.Instance.SendToClient(
                    client,
                    Service.ShowNotification("Không tải được shop vật phẩm.")
                );

                return;
            }

            ServerNetwork.Instance.SendToClient(
                client,
                Service.SendSkinAndCharacterBundleResponse(bundles)
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[SKIN BUNDLE] {e}");
        }
    }

    private async Task HandlePurchaseSkinBundle(
    Client client,
    string payload)
    {
        try
        {
            PurchaseOrderRequest req =
                JsonConvert.DeserializeObject
                <PurchaseOrderRequest>(payload);

            PurchaseOrderResponse response =
                await ApiService.PurchaseSkinBundle(
                    client,
                    req.skinAndCharacterBundleId
                );

            string json =
                JsonConvert.SerializeObject(response);

            ServerNetwork.Instance.SendToClient(
                client,
                ReliableMessage.Build(
                    Command.PurchaseSkinBundleResponse,
                    Encoding.UTF8.GetBytes(json)
                )
            );
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}