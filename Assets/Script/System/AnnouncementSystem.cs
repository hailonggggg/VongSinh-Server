using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

public class AnnouncementSystem : BaseSystem
{
    public override void HandlePackage(Client client, Command messageType, string payload)
    {
        switch (messageType)
        {
            case Command.RequestAnnouncement:
                _ = HandleRequestAnnouncement(client);
                break;
        }
    }

    // =========================
    // 📡 HANDLE REQUEST
    // =========================
    private async Task HandleRequestAnnouncement(Client client)
    {
        try
        {
            if (client == null)
            {
                Debug.LogWarning("[ANNOUNCEMENT] Client is null");
                return;
            }

            if (string.IsNullOrEmpty(client.Token))
            {
                Debug.LogWarning("[ANNOUNCEMENT] Client token is missing");
                ServerNetwork.Instance.SendToClient(
                    client,
                    Service.ShowNotification("Bạn chưa đăng nhập hoặc token không hợp lệ.")
                );
                return;
            }

            Debug.Log($"[ANNOUNCEMENT] Fetching announcements for {client.User?.LastName}");

            AnnouncementResponse[] announcements =
                await ApiService.GetAllAnnouncement(client);

            if (announcements == null)
            {
                Debug.LogWarning("[ANNOUNCEMENT] API returned null");

                ServerNetwork.Instance.SendToClient(
                    client,
                    Service.ShowNotification("Không thể tải thông báo.")
                );
                return;
            }

            Debug.Log($"[ANNOUNCEMENT] Sending {announcements.Length} announcements to client");

            ServerNetwork.Instance.SendToClient(
                client,
                Service.SendAnnouncementResponse(announcements)
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[ANNOUNCEMENT] Unexpected error: {e}");

            ServerNetwork.Instance.SendToClient(
                client,
                Service.ShowNotification("Lỗi khi tải thông báo.")
            );
        }
    }
}