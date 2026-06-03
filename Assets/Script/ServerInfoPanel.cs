using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ServerInfoPanel : MonoBehaviour
{
    [Header("Root (leave empty to auto-create)")]
    [SerializeField] private GameObject panelRoot;

    [Header("Header")]
    [SerializeField] private Text titleText;

    [Header("Global Stats")]
    [SerializeField] private Text totalClientsText;
    [SerializeField] private Text totalRoomsText;
    [SerializeField] private Text pendingMatchesText;
    [SerializeField] private Text uptimeText;

    [Header("Sections")]
    [SerializeField] private Transform clientsSection;
    [SerializeField] private Transform roomsSection;
    [SerializeField] private Transform pendingSection;

    [Header("Prefabs (leave empty to auto-create)")]
    [SerializeField] private GameObject clientRowPrefab;
    [SerializeField] private GameObject roomRowPrefab;
    [SerializeField] private GameObject pendingRowPrefab;

    [Header("Settings")]
    [SerializeField] private float refreshInterval = 1f;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private float _timer;
    private bool _visible;
    private float _startTime;

    private void Awake()
    {
        _startTime = Time.realtimeSinceStartup;
        if (panelRoot == null)
        {
            BuildCanvasHierarchy();
        }
        _visible = panelRoot != null && panelRoot.activeSelf;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _visible = !_visible;
            if (panelRoot != null)
            {
                panelRoot.SetActive(_visible);
            }
        }

        if (!_visible || panelRoot == null) return;

        _timer += Time.deltaTime;
        if (_timer >= refreshInterval)
        {
            _timer = 0;
            Refresh();
        }
    }

    private void Refresh()
    {
        if (Master.Instance == null)
        {
            if (titleText != null)
            {
                titleText.text = "Server Info\n(Master not found)";
            }
            return;
        }

        var clients = ClientManager.AllClients.Values.ToList();
        var rooms = RoomSystem.AllRooms.ToList();
        var pending = RoomSystem.AllPendingMatches;

        float uptime = Time.realtimeSinceStartup - _startTime;
        string uptimeStr = FormatTime(uptime);

        if (titleText != null) titleText.text = "SERVER INFO";
        if (totalClientsText != null) totalClientsText.text = $"Clients: {clients.Count}";
        if (totalRoomsText != null) totalRoomsText.text = $"Rooms: {rooms.Count}";
        if (pendingMatchesText != null) pendingMatchesText.text = $"Pending Matches: {pending.Count}";
        if (uptimeText != null) uptimeText.text = $"Uptime: {uptimeStr}";

        RebuildSection(clientsSection, clients, clientRowPrefab, BuildClientRow);
        RebuildSection(roomsSection, rooms, roomRowPrefab, BuildRoomRow);
        RebuildSection(pendingSection, pending.Values.ToList(), pendingRowPrefab, BuildPendingRow);
    }

    private void RebuildSection<T>(Transform section, List<T> items, GameObject prefab, Func<Transform, T, GameObject> builder)
    {
        if (section == null) return;

        for (int i = section.childCount - 1; i >= 0; i--)
        {
            Destroy(section.GetChild(i).gameObject);
        }

        if (prefab == null) return;

        foreach (var item in items)
        {
            var row = Instantiate(prefab, section).GetComponent<Transform>();
            builder(row, item);
        }
    }

    private GameObject BuildClientRow(Transform row, Client client)
    {
        var texts = row.GetComponentsInChildren<Text>(true);
        if (texts.Length >= 4)
        {
            texts[0].text = client.PlayerRef.PlayerId.ToString();
            texts[1].text = client.Name;
            texts[2].text = client.CurrentRoomId > 0 ? $"Room {client.CurrentRoomId}" : "Lobby";
            texts[3].text = client.CurrentBattleId > 0 ? $"Battle {client.CurrentBattleId}" : "-";
        }
        return row.gameObject;
    }

    private GameObject BuildRoomRow(Transform row, Room room)
    {
        var texts = row.GetComponentsInChildren<Text>(true);
        if (texts.Length >= 4)
        {
            texts[0].text = room.RoomId.ToString();
            texts[1].text = room.Name;
            texts[2].text = $"{room.Players.Count}/{room.MaxPlayers}";
            texts[3].text = $"Map {room.MapIndexSelected}";
        }
        return row.gameObject;
    }

    private GameObject BuildPendingRow(Transform row, RoomSystem.PendingMatch match)
    {
        var texts = row.GetComponentsInChildren<Text>(true);
        if (texts.Length >= 3)
        {
            texts[0].text = match.Player1?.Name ?? "?";
            texts[1].text = match.Player2?.Name ?? "Waiting...";
            texts[2].text = $"{match.Player1Ready} / {match.Player2Ready}";
        }
        return row.gameObject;
    }

    private static string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }

    private void BuildCanvasHierarchy()
    {
        var canvasGO = new GameObject("ServerInfoCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(canvasGO.transform, false);
        var panelRt = panelRoot.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0, 0.5f);
        panelRt.anchorMax = new Vector2(0.35f, 1f);
        panelRt.offsetMin = new Vector2(10, 10);
        panelRt.offsetMax = new Vector2(-10, -10);

        var panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);

        var scroll = new GameObject("ScrollView");
        scroll.transform.SetParent(panelRoot.transform, false);
        var scrollRt = scroll.AddComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(10, 50);
        scrollRt.offsetMax = new Vector2(-10, -10);

        var scrollView = scroll.AddComponent<ScrollRect>();
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scroll.transform, false);
        var viewportRt = viewport.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;
        var vpImage = viewport.AddComponent<Image>();
        vpImage.color = Color.clear;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.offsetMin = new Vector2(0, 0);
        contentRt.offsetMax = new Vector2(0, 0);

        scrollView.viewport = viewportRt;
        scrollView.content = contentRt;
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.scrollSensitivity = 20;

        clientsSection = new GameObject("ClientsSection").transform;
        clientsSection.SetParent(content.transform, false);
        var csRt = clientsSection.gameObject.AddComponent<RectTransform>();
        csRt.anchorMin = new Vector2(0, 1);
        csRt.anchorMax = new Vector2(1, 1);
        csRt.pivot = new Vector2(0.5f, 1);
        csRt.offsetMin = new Vector2(0, 0);
        csRt.offsetMax = new Vector2(0, 0);

        roomsSection = new GameObject("RoomsSection").transform;
        roomsSection.SetParent(content.transform, false);
        var rsRt = roomsSection.gameObject.AddComponent<RectTransform>();
        rsRt.anchorMin = new Vector2(0, 1);
        rsRt.anchorMax = new Vector2(1, 1);
        rsRt.pivot = new Vector2(0.5f, 1);
        rsRt.offsetMin = new Vector2(0, 0);
        rsRt.offsetMax = new Vector2(0, 0);

        pendingSection = new GameObject("PendingSection").transform;
        pendingSection.SetParent(content.transform, false);
        var psRt = pendingSection.gameObject.AddComponent<RectTransform>();
        psRt.anchorMin = new Vector2(0, 1);
        psRt.anchorMax = new Vector2(1, 1);
        psRt.pivot = new Vector2(0.5f, 1);
        psRt.offsetMin = new Vector2(0, 0);
        psRt.offsetMax = new Vector2(0, 0);

        titleText = CreateLabel("Title", panelRoot.transform, new Vector2(10, -10), new Vector2(-10, -40), 18, FontStyle.Bold, Color.white);
        totalClientsText = CreateLabel("Clients", panelRoot.transform, new Vector2(10, -45), new Vector2(-10, -65), 14, FontStyle.Normal, Color.cyan);
        totalRoomsText = CreateLabel("Rooms", panelRoot.transform, new Vector2(10, -68), new Vector2(-10, -88), 14, FontStyle.Normal, Color.green);
        pendingMatchesText = CreateLabel("Pending", panelRoot.transform, new Vector2(10, -91), new Vector2(-10, -111), 14, FontStyle.Normal, Color.yellow);
        uptimeText = CreateLabel("Uptime", panelRoot.transform, new Vector2(10, -114), new Vector2(-10, -134), 14, FontStyle.Normal, Color.gray);

        clientRowPrefab = CreateRowPrefab("ClientRowPrefab", new[] { "Id", "Name", "Room", "Battle" });
        roomRowPrefab = CreateRowPrefab("RoomRowPrefab", new[] { "Id", "Name", "Players", "Map" });
        pendingRowPrefab = CreateRowPrefab("PendingRowPrefab", new[] { "P1", "P2", "Ready" });

        var hint = CreateLabel("Hint", panelRoot.transform, new Vector2(10, -145), new Vector2(-10, -165), 12, FontStyle.Italic, Color.gray);
        hint.text = "Press F1 to toggle";

        panelRoot.SetActive(true);
        _visible = true;
    }

    private Text CreateLabel(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax, int fontSize, FontStyle style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        return text;
    }

    private GameObject CreateRowPrefab(string name, string[] columnHeaders)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 24);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.05f);

        float colWidth = 1f / columnHeaders.Length;
        for (int i = 0; i < columnHeaders.Length; i++)
        {
            var cell = new GameObject($"Cell{i}");
            cell.transform.SetParent(go.transform, false);
            var cellRt = cell.AddComponent<RectTransform>();
            cellRt.anchorMin = new Vector2(i * colWidth, 0);
            cellRt.anchorMax = new Vector2((i + 1) * colWidth, 1);
            cellRt.offsetMin = new Vector2(2, 2);
            cellRt.offsetMax = new Vector2(-2, -2);

            var cellText = cell.AddComponent<Text>();
            cellText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cellText.fontSize = 12;
            cellText.color = Color.white;
            cellText.alignment = TextAnchor.MiddleLeft;
        }

        go.SetActive(false);
        return go;
    }
}
