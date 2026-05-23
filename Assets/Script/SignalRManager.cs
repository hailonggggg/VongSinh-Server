using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using UnityEngine;

public class SignalRManager : MonoBehaviour
{
    public static SignalRManager Instance { get; private set; }
    private HubConnection connection;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        connection = new HubConnectionBuilder()
            .WithUrl("https://be-adminmanagementsystem.onrender.com/annoucementHub")
            .WithAutomaticReconnect()
            .Build();

        connection.On<string>("SendNewAnnoucement", (json) =>
        {
            Debug.Log($"[SIGNALR] Raw JSON: {json}");

            try
            {
                AnnouncementResponse announcement = JsonConvert.DeserializeObject<AnnouncementResponse>(json);

                if (announcement == null)
                {
                    Debug.LogWarning("[SIGNALR] Parsed announcement is null");
                    return;
                }

                Debug.Log($"[SIGNALR] Parsed title: {announcement.title}");

                MainThreadDispatcher.Enqueue(() =>
                {
                    byte[] packet = Service.SendRealTimeAnnouncement(announcement);
                    ServerNetwork.Instance.BroadcastToAllClients(packet);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[SIGNALR] Parse error: {e.Message}");
            }
        });

        try
        {
            await connection.StartAsync();
            // await connection.InvokeAsync("JoinAnnouncementGroup", "announcements");
            Debug.Log("[SIGNALR] Connected and joined announcements group");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SIGNALR] Connection failed: {e.Message}");
        }
    }

    async void OnDestroy()
    {
        if (connection != null)
        {
            await connection.StopAsync();
        }
    }
}