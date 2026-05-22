using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;

public class SignalRManager : MonoBehaviour
{
    private HubConnection connection;
    async void Start()
    {
        connection = new HubConnectionBuilder()
            .WithUrl("https://be-adminmanagementsystem.onrender.com/hubs/announcement")
            .WithAutomaticReconnect()
            .Build();

        connection.On<string>("ReceiveAnnouncement", (announcemenJson) =>
        {
        });

        await connection.StartAsync();

        Debug.Log("Connected");
    }

    async void OnDestroy()
    {
        if (connection != null)
        {
            await connection.StopAsync();
        }
    }
}
