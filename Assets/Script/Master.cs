using System;
using Assets.Script.System;
using UnityEngine;

public class Master : MonoBehaviour
{
    public static Master Instance;
    private AuthSystem authSystem;
    private RoomSystem roomSystem;
    private BattleSystem battleSystem;
    private AnnouncementSystem announcementSystem;
    private BundleSystem bundleSystem;
    private OrderSystem orderSystem;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        authSystem = new AuthSystem();
        roomSystem = new RoomSystem();
        battleSystem = new BattleSystem();
        announcementSystem =  new AnnouncementSystem();
        bundleSystem = new BundleSystem();
        orderSystem = new OrderSystem();
    }

    void Update()
    {
        battleSystem?.Tick(Time.deltaTime);
    }


    public void ClearClientResource(Client client)
    {
        if (client == null || client.CurrentRoomId < 0)
        {
            return;
        }

        if (RoomSystem.TryGetRoomById(client.CurrentRoomId, out Room room))
        {
            roomSystem.LeaveRoom(client);
        }
    }
}
