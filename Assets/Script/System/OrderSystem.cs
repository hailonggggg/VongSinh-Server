using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class OrderSystem : BaseSystem
{
    public override void HandlePackage(Client client, Command messageType, string payload)
    {
        switch (messageType)
        {
            case Command.RequestBuyBundle:
                _ = HandleBuy(client, payload);
                break;
            case Command.RequestOrderStatus:
                _ = HandleCheckOrder(client, payload);
                break;
        }
    }

    private async Task HandleBuy(Client client, string payload)
    {
        try
        {

            Debug.Log($"[ORDER] Received buy request: {payload}");

            GemBundleResponse bundle =
                JsonConvert.DeserializeObject<GemBundleResponse>(payload);

            OrderResponse order =
                await ApiService.CreateOrder(client, bundle);

            if (order == null)
            {
                ServerNetwork.Instance.SendToClient(
                    client,
                    Service.ShowNotification("Tạo đơn hàng thất bại")
                );
                return;
            }

            ServerNetwork.Instance.SendToClient(
                client,
                Service.SendOrderResponse(order)
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[ORDER] {e}");
        }
    }

    private async Task HandleCheckOrder(Client client, string payload)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, int>>(payload);
            int orderId = data["id"];

            OrderResponse order =
                await ApiService.GetOrderById(client, orderId);

            if (order == null)
                return;

            if (!string.IsNullOrEmpty(order.status) &&
                order.status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("[SERVER] Order PAID → notify client");

                ServerNetwork.Instance.SendToClient(
                    client,
                    Service.SendOrderResponse(order)
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ORDER CHECK] {e}");
        }
    }
}