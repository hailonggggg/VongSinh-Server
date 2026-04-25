using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

public class InventorySystem : BaseSystem
{
    public override void HandlePackage(Client client, Command messageType, string payload)
    {
        switch (messageType)
        {
            case Command.RequestInventory:
                _ = HandleRequestInventory(client);
                break;
        }
    }

    private async Task HandleRequestInventory(Client client)
    {
        try
        {
            if (client == null)
            {
                Debug.LogWarning("[INVENTORY] Client null");
                return;
            }

            if (string.IsNullOrEmpty(client.Token))
            {
                Debug.LogWarning("[INVENTORY] Missing token");

                ServerNetwork.Instance.SendToClient(
                    client,
                    Service.ShowNotification("Bạn chưa đăng nhập.")
                );
                return;
            }

            Debug.Log($"[INVENTORY] Fetching for {client.User?.LastName}");

            UserItem[] items = await ApiService.GetInventory(client);

            if (items == null)
            {
                ServerNetwork.Instance.SendToClient(
                    client,
                    Service.ShowNotification("Không thể tải inventory.")
                );
                return;
            }

            Debug.Log($"[INVENTORY] Send {items.Length} items");

            ServerNetwork.Instance.SendToClient(
                client,
                Service.SendInventoryResponse(items)
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[INVENTORY] Error: {e}");

            ServerNetwork.Instance.SendToClient(
                client,
                Service.ShowNotification("Lỗi khi tải inventory.")
            );
        }
    }
}