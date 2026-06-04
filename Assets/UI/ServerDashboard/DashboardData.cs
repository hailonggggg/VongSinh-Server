using System;
using Unity.Properties;
using UnityEngine;

[Serializable]
public class DashboardData
{
    [CreateProperty]
    public string Uptime { get; set; } = "00:00:00";

    [CreateProperty]
    public int TotalClients { get; set; }

    [CreateProperty]
    public int ActiveRooms { get; set; }

    [CreateProperty]
    public int PendingMatches { get; set; }

    [CreateProperty]
    public int BattlesInProgress { get; set; }

    [CreateProperty]
    public string ServerStatus { get; set; } = "Running";
}
