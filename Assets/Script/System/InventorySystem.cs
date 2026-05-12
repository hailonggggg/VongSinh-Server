using System;
using System.Collections.Generic;
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

            UserItem[] items = await ApiService.GetInventory(client);

            if (items == null)
            {
                ServerNetwork.Instance.SendToClient(
                    client,
                    Service.ShowNotification("Không thể tải inventory.")
                );
                return;
            }

            List<Task<ItemDetail>> detailTasks = new List<Task<ItemDetail>>();
            foreach (var item in items)
                detailTasks.Add(ApiService.GetItemById(client, item.itemId));

            ItemDetail[] details = await Task.WhenAll(detailTasks);

            List<UserItemWithDetail> result = new List<UserItemWithDetail>();
            for (int i = 0; i < items.Length; i++)
            {
                var detail = details[i];
                result.Add(new UserItemWithDetail
                {
                    userId = items[i].userId,
                    itemId = items[i].itemId,
                    quantity = items[i].quantity,
                    shopOrderId = items[i].shopOrderId,
                    itemName = detail?.itemName ?? $"Item {items[i].itemId}",
                    itemDescription = detail?.itemDescription ?? "",
                    itemImageUrl = detail?.itemImageUrl ?? "",
                    itemStatus = detail?.status ?? ""
                });
            }

            Debug.Log($"[INVENTORY] Sending {result.Count} items with detail");

            ServerNetwork.Instance.SendToClient(
                client,
                Service.SendInventoryResponse(result.ToArray())
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