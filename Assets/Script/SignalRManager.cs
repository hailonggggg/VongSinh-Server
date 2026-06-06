using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

public class SignalRManager : MonoBehaviour
{
    public static SignalRManager Instance { get; private set; }
    private HubConnection connection;

    private static AnnouncementResponse ParseAnnouncement(string json)
    {
        JObject obj = JObject.Parse(json);
        return new AnnouncementResponse
        {
            announcementId = obj["AnnouncementId"]?.Value<int>() ?? obj["announcementId"]?.Value<int>() ?? 0,
            title = obj["Title"]?.Value<string>() ?? obj["title"]?.Value<string>() ?? "",
            content = obj["Content"]?.Value<string>() ?? obj["content"]?.Value<string>() ?? "",
            type = obj["Type"]?.Value<string>() ?? obj["type"]?.Value<string>() ?? "",
            status = obj["Status"]?.Value<string>() ?? obj["status"]?.Value<string>() ?? "",
            startDate = obj["StartDate"]?.Value<DateTime>() ?? obj["startDate"]?.Value<DateTime>() ?? default,
            endDate = obj["EndDate"]?.Value<DateTime>() ?? obj["endDate"]?.Value<DateTime>() ?? default,
            createdBy = obj["CreatedBy"]?.Value<int>() ?? obj["createdBy"]?.Value<int>() ?? 0,
            createdAt = obj["CreatedAt"]?.Value<DateTime>() ?? obj["createdAt"]?.Value<DateTime>() ?? default,
            updatedBy = obj["UpdatedBy"]?.Value<int?>() ?? obj["updatedBy"]?.Value<int?>(),
            updatedAt = obj["UpdatedAt"]?.Value<DateTime?>() ?? obj["updatedAt"]?.Value<DateTime?>(),
        };
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _ = StartConnection();
    }

    private async System.Threading.Tasks.Task StartConnection()
    {
        connection = new HubConnectionBuilder()
            .WithUrl("https://be-adminmanagementsystem.onrender.com/annoucementHub")
            .WithAutomaticReconnect()
            .Build();

        connection.On<string>("AnnouncementCreated", (json) =>
        {
            Debug.Log($"[SIGNALR] AnnouncementCreated raw: {json}");
            try
            {
                AnnouncementResponse announcement = ParseAnnouncement(json);
                Debug.Log($"[SIGNALR] Parsed Created id={announcement.announcementId} title={announcement.title}");
                MainThreadDispatcher.Enqueue(() =>
                {
                    byte[] packet = Service.SendRealTimeAnnouncementCreated(announcement);
                    ServerNetwork.Instance.BroadcastToAllClients(packet);
                });
            }
            catch (Exception e) { Debug.LogError($"[SIGNALR] AnnouncementCreated parse error: {e.Message}"); }
        });

        connection.On<string>("AnnouncementUpdated", (json) =>
        {
            Debug.Log($"[SIGNALR] AnnouncementUpdated raw: {json}");
            try
            {
                AnnouncementResponse announcement = ParseAnnouncement(json);
                Debug.Log($"[SIGNALR] Parsed Updated id={announcement.announcementId} title={announcement.title}");
                Debug.Log($"[SIGNALR] Enqueueing broadcast action...");
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log($"[SIGNALR] MainThread: ServerNetwork null={ServerNetwork.Instance == null}");
                    if (ServerNetwork.Instance == null) { Debug.LogError("[SIGNALR] ServerNetwork NULL"); return; }

                    byte[] packet = Service.SendRealTimeAnnouncementUpdated(announcement);
                    Debug.Log($"[SIGNALR] Broadcasting packet length={packet?.Length}");
                    ServerNetwork.Instance.BroadcastToAllClients(packet);
                    Debug.Log($"[SIGNALR] Broadcast done");
                });
            }
            catch (Exception e) { Debug.LogError($"[SIGNALR] AnnouncementUpdated parse error: {e.Message}"); }
        });

        connection.On<string>("AnnouncementDeleted", (json) =>
        {
            Debug.Log($"[SIGNALR] AnnouncementDeleted raw: {json}");
            try
            {
                JObject obj = JObject.Parse(json);
                int id = obj["announcementId"]?.Value<int>() ?? obj["AnnouncementId"]?.Value<int>() ?? 0;
                Debug.Log($"[SIGNALR] Parsed Deleted id={id}");
                MainThreadDispatcher.Enqueue(() =>
                {
                    byte[] packet = Service.SendRealTimeAnnouncementDeleted(id);
                    ServerNetwork.Instance.BroadcastToAllClients(packet);
                });
            }
            catch (Exception e) { Debug.LogError($"[SIGNALR] AnnouncementDeleted parse error: {e.Message}"); }
        });

        connection.On<string>("SendNewAnnoucement", (json) =>
        {
            try
            {
                AnnouncementResponse announcement = ParseAnnouncement(json);
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log($"[SIGNALR] MainThread executing broadcast, ServerNetwork={ServerNetwork.Instance != null}");
                    if (ServerNetwork.Instance == null)
                    {
                        Debug.LogError("[SIGNALR] ServerNetwork.Instance is NULL!");
                        return;
                    }
                    byte[] packet = Service.SendRealTimeAnnouncementUpdated(announcement);
                    Debug.Log($"[SIGNALR] Packet built, length={packet?.Length}, broadcasting...");
                    ServerNetwork.Instance.BroadcastToAllClients(packet);
                    Debug.Log($"[SIGNALR] BroadcastToAllClients called");
                });
            }
            catch (Exception e) { Debug.LogError($"[SIGNALR] SendNewAnnoucement parse error: {e.Message}"); }
        });

        try
        {
            await connection.StartAsync();
            Debug.Log("[SIGNALR] Connected to announcement hub");
        }
        catch (Exception e) { Debug.LogError($"[SIGNALR] Connection failed: {e.Message}"); }
    }

    async void OnDestroy()
    {
        if (connection != null) await connection.StopAsync();
    }
}