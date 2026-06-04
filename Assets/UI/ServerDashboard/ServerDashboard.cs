using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Script.System;
using UnityEngine;
using UnityEngine.UIElements;

public class ServerDashboard : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float refreshInterval = 1f;

    private DashboardData _data = new();
    private float _timer;
    private float _startTime;

    private VisualElement _root;
    private Label _activeTabTitle;
    private VisualElement _pageOverview;
    private VisualElement _pageClients;
    private VisualElement _pageRooms;
    private VisualElement _pageMatchmaking;

    private ListView _clientsList;
    private ListView _roomsList;
    private ListView _pendingList;

    private List<Button> _tabButtons = new();
    private Dictionary<Button, VisualElement> _tabToPage = new();

    private void Awake()
    {
        _startTime = Time.realtimeSinceStartup;
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        _root = uiDocument.rootVisualElement;
        _root.dataSource = _data;

        // Tabs
        var tabOverview = _root.Q<Button>("tabOverview");
        var tabClients = _root.Q<Button>("tabClients");
        var tabRooms = _root.Q<Button>("tabRooms");
        var tabMatchmaking = _root.Q<Button>("tabMatchmaking");

        _activeTabTitle = _root.Q<Label>("activeTabTitle");
        _pageOverview = _root.Q("pageOverview");
        _pageClients = _root.Q("pageClients");
        _pageRooms = _root.Q("pageRooms");
        _pageMatchmaking = _root.Q("pageMatchmaking");

        _tabToPage[tabOverview] = _pageOverview;
        _tabToPage[tabClients] = _pageClients;
        _tabToPage[tabRooms] = _pageRooms;
        _tabToPage[tabMatchmaking] = _pageMatchmaking;

        _tabButtons.AddRange(new[] { tabOverview, tabClients, tabRooms, tabMatchmaking });

        foreach (var btn in _tabButtons)
        {
            btn.clicked += () => SwitchTab(btn);
        }

        // ListViews
        SetupClientsList();
        SetupRoomsList();
        SetupPendingList();

        Refresh();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= refreshInterval)
        {
            _timer = 0;
            Refresh();
        }
    }

    private void SwitchTab(Button clickedTab)
    {
        foreach (var btn in _tabButtons)
        {
            btn.RemoveFromClassList("tab-button--active");
        }
        clickedTab.AddToClassList("tab-button--active");

        foreach (var page in _tabToPage.Values)
        {
            page.AddToClassList("hidden");
        }
        _tabToPage[clickedTab].RemoveFromClassList("hidden");
        _activeTabTitle.text = clickedTab.text;
    }

    private void Refresh()
    {
        if (Master.Instance == null)
        {
            _data.ServerStatus = "Master Offline";
            return;
        }

        _data.ServerStatus = "Running";
        _data.Uptime = FormatTime(Time.realtimeSinceStartup - _startTime);
        
        var clients = ClientManager.AllClients.Values.Where(c => c != null).ToList();
        _data.TotalClients = clients.Count;
        
        var rooms = RoomSystem.AllRooms.ToList();
        _data.ActiveRooms = rooms.Count;
        
        var pending = RoomSystem.AllPendingMatches.Values.ToList();
        _data.PendingMatches = pending.Count;

        _data.BattlesInProgress = BattleSystem.AllBattles.Count();

        // Update Lists
        _clientsList.itemsSource = clients;
        _clientsList.Rebuild();

        _roomsList.itemsSource = rooms;
        _roomsList.Rebuild();

        _pendingList.itemsSource = pending;
        _pendingList.Rebuild();
    }

    private void SetupClientsList()
    {
        _clientsList = _root.Q<ListView>("clientsList");
        _clientsList.makeItem = () => {
            var row = new VisualElement();
            row.AddToClassList("row-item");
            row.Add(new Label { name = "id" });
            row.Add(new Label { name = "name" });
            row.Add(new Label { name = "room" });
            row.Add(new Label { name = "battle" });
            
            row.Q<Label>("id").AddToClassList("cell");
            row.Q<Label>("name").AddToClassList("cell");
            row.Q<Label>("name").style.flexGrow = 2;
            row.Q<Label>("room").AddToClassList("cell");
            row.Q<Label>("battle").AddToClassList("cell");
            return row;
        };
        _clientsList.bindItem = (element, i) => {
            var client = (Client)_clientsList.itemsSource[i];
            element.Q<Label>("id").text = client.PlayerRef.PlayerId.ToString();
            element.Q<Label>("name").text = client.User?.LastName ?? "Unknown";
            element.Q<Label>("room").text = client.CurrentRoomId > 0 ? $"Room {client.CurrentRoomId}" : "Lobby";
            element.Q<Label>("battle").text = client.CurrentBattleId > 0 ? $"Battle {client.CurrentBattleId}" : "-";
        };
    }

    private void SetupRoomsList()
    {
        _roomsList = _root.Q<ListView>("roomsList");
        _roomsList.makeItem = () => {
            var row = new VisualElement();
            row.AddToClassList("row-item");
            row.Add(new Label { name = "id" });
            row.Add(new Label { name = "name" });
            row.Add(new Label { name = "players" });
            row.Add(new Label { name = "map" });
            
            row.Q<Label>("id").AddToClassList("cell");
            row.Q<Label>("name").AddToClassList("cell");
            row.Q<Label>("name").style.flexGrow = 2;
            row.Q<Label>("players").AddToClassList("cell");
            row.Q<Label>("map").AddToClassList("cell");
            return row;
        };
        _roomsList.bindItem = (element, i) => {
            var room = (Room)_roomsList.itemsSource[i];
            element.Q<Label>("id").text = room.RoomId.ToString();
            element.Q<Label>("name").text = room.Name;
            element.Q<Label>("players").text = $"{room.Players?.Count ?? 0}/{room.MaxPlayers}";
            element.Q<Label>("map").text = room.MapIndexSelected >= 0 ? $"Map {room.MapIndexSelected}" : "-";
        };
    }

    private void SetupPendingList()
    {
        _pendingList = _root.Q<ListView>("pendingList");
        _pendingList.makeItem = () => {
            var row = new VisualElement();
            row.AddToClassList("row-item");
            row.Add(new Label { name = "p1" });
            row.Add(new Label { name = "p2" });
            row.Add(new Label { name = "ready" });
            
            row.Q<Label>("p1").AddToClassList("cell");
            row.Q<Label>("p2").AddToClassList("cell");
            row.Q<Label>("ready").AddToClassList("cell");
            return row;
        };
        _pendingList.bindItem = (element, i) => {
            var match = (RoomSystem.PendingMatch)_pendingList.itemsSource[i];
            element.Q<Label>("p1").text = match.Player1?.User?.LastName ?? "?";
            element.Q<Label>("p2").text = match.Player2?.User?.LastName ?? "Waiting";
            element.Q<Label>("ready").text = $"{match.Player1Ready}/{match.Player2Ready}";
        };
    }

    private string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }
}
