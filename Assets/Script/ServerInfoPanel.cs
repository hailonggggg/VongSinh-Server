using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ServerInfoPanel : MonoBehaviour
{
    [Header("Root (leave empty to auto-create)")]
    [SerializeField] private GameObject panelRoot;

    [Header("Global Stats")]
    [SerializeField] private Text totalClientsText;
    [SerializeField] private Text totalRoomsText;
    [SerializeField] private Text pendingMatchesText;
    [SerializeField] private Text inBattleText;
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

    private readonly List<GameObject> _clientRows = new();
    private readonly List<GameObject> _roomRows = new();
    private readonly List<GameObject> _pendingRows = new();

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
                titleText.text = "SERVER INFO\n(Master not found)";
            }
            return;
        }

        var clients = ClientManager.AllClients.Values
            .Where(c => c != null)
            .OrderBy(c => c.PlayerRef.PlayerId)
            .ToList();
        var rooms = RoomSystem.AllRooms.OrderBy(r => r.RoomId).ToList();
        var pending = RoomSystem.AllPendingMatches.Values
            .OrderBy(m => m.CreatedAt)
            .ToList();
        int inBattle = clients.Count(c => c.CurrentBattleId > 0);

        float uptime = Time.realtimeSinceStartup - _startTime;
        string uptimeStr = FormatTime(uptime);

        if (titleText != null) titleText.text = "SERVER INFO";
        if (totalClientsText != null) totalClientsText.text = $"Clients: {clients.Count}";
        if (totalRoomsText != null) totalRoomsText.text = $"Rooms: {rooms.Count}";
        if (pendingMatchesText != null) pendingMatchesText.text = $"Pending: {pending.Count}";
        if (inBattleText != null) inBattleText.text = $"In Battle: {inBattle}";
        if (uptimeText != null) uptimeText.text = $"Uptime: {uptimeStr}";

        RebuildSection(clientsSection, _clientRows, clients, clientRowPrefab, BuildClientRow);
        RebuildSection(roomsSection, _roomRows, rooms, roomRowPrefab, BuildRoomRow);
        RebuildSection(pendingSection, _pendingRows, pending, pendingRowPrefab, BuildPendingRow);

        UpdateSectionHeader(clientsSectionHeader, "CLIENTS", clients.Count);
        UpdateSectionHeader(roomsSectionHeader, "ROOMS", rooms.Count);
        UpdateSectionHeader(pendingSectionHeader, "PENDING MATCHES", pending.Count);
    }

    private void RebuildSection<T>(
        Transform section,
        List<GameObject> rowPool,
        IReadOnlyList<T> items,
        GameObject prefab,
        Action<Transform, T> builder)
    {
        if (section == null || prefab == null) return;

        int i = 0;
        for (; i < items.Count; i++)
        {
            GameObject row;
            if (i < rowPool.Count)
            {
                row = rowPool[i];
                row.SetActive(true);
            }
            else
            {
                row = Instantiate(prefab, section);
                rowPool.Add(row);
            }
            builder(row.transform, items[i]);
        }
        for (; i < rowPool.Count; i++)
        {
            rowPool[i].SetActive(false);
        }
    }

    private void BuildClientRow(Transform row, Client client)
    {
        var texts = row.GetComponentsInChildren<Text>(true);
        if (texts.Length >= 4)
        {
            texts[0].text = client.PlayerRef.PlayerId.ToString();
            texts[1].text = Truncate(client.Name, 18);
            texts[2].text = client.CurrentRoomId > 0 ? $"Room {client.CurrentRoomId}" : "Lobby";
            texts[3].text = client.CurrentBattleId > 0 ? $"Battle {client.CurrentBattleId}" : "-";
        }
    }

    private void BuildRoomRow(Transform row, Room room)
    {
        var texts = row.GetComponentsInChildren<Text>(true);
        if (texts.Length >= 4)
        {
            texts[0].text = room.RoomId.ToString();
            texts[1].text = string.IsNullOrEmpty(room.Name) ? "-" : Truncate(room.Name, 20);
            texts[2].text = $"{room.Players?.Count ?? 0}/{room.MaxPlayers}";
            texts[3].text = room.MapIndexSelected >= 0 ? $"Map {room.MapIndexSelected}" : "-";
        }
    }

    private void BuildPendingRow(Transform row, RoomSystem.PendingMatch match)
    {
        var texts = row.GetComponentsInChildren<Text>(true);
        if (texts.Length >= 3)
        {
            texts[0].text = Truncate(match.Player1?.Name ?? "?", 14);
            texts[1].text = Truncate(match.Player2?.Name ?? "Waiting...", 14);
            var age = match.CreatedAt != default
                ? (DateTime.UtcNow - match.CreatedAt).TotalSeconds.ToString("F0") + "s"
                : "-";
            texts[2].text = $"{match.Player1Ready}/{match.Player2Ready} ({age})";
        }
    }

    private static string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        if (t.TotalHours >= 100)
            return $"{(int)t.TotalHours}h {t.Minutes:D2}m {t.Seconds:D2}s";
        return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
        return s[..(max - 1)] + "…";
    }

    [Header("Auto-created references (do not assign)")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text clientsSectionHeader;
    [SerializeField] private Text roomsSectionHeader;
    [SerializeField] private Text pendingSectionHeader;

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
        panelBg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

        float statsBlockHeight = 130f;

        var scroll = new GameObject("ScrollView");
        scroll.transform.SetParent(panelRoot.transform, false);
        var scrollRt = scroll.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0);
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(10, 10);
        scrollRt.offsetMax = new Vector2(-10, -(statsBlockHeight + 10));

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

        titleText = CreateLabel("Title", panelRoot.transform,
            new Vector2(10, -10), new Vector2(-10, -36), 18, FontStyle.Bold, Color.white);

        totalClientsText = CreateStatLabel("Clients", panelRoot.transform,
            new Vector2(10, -42), new Vector2(-10, -58), Color.cyan);
        totalRoomsText = CreateStatLabel("Rooms", panelRoot.transform,
            new Vector2(10, -62), new Vector2(-10, -78), Color.green);
        pendingMatchesText = CreateStatLabel("Pending", panelRoot.transform,
            new Vector2(10, -82), new Vector2(-10, -98), Color.yellow);
        inBattleText = CreateStatLabel("InBattle", panelRoot.transform,
            new Vector2(10, -102), new Vector2(-10, -118), new Color(1f, 0.6f, 0.3f));
        uptimeText = CreateStatLabel("Uptime", panelRoot.transform,
            new Vector2(10, -122), new Vector2(-10, -140), Color.gray);

        var divider = new GameObject("Divider");
        divider.transform.SetParent(panelRoot.transform, false);
        var divRt = divider.AddComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0, 1);
        divRt.anchorMax = new Vector2(1, 1);
        divRt.pivot = new Vector2(0.5f, 1);
        divRt.offsetMin = new Vector2(10, -148);
        divRt.offsetMax = new Vector2(-10, -150);
        var divImg = divider.AddComponent<Image>();
        divImg.color = new Color(1, 1, 1, 0.15f);

        var hint = CreateLabel("Hint", panelRoot.transform,
            new Vector2(10, -158), new Vector2(-10, -172), 11, FontStyle.Italic, new Color(1, 1, 1, 0.4f));
        hint.text = "F1: Toggle";

        clientsSection = CreateSection("ClientsSection", content.transform, out clientsSectionHeader);
        roomsSection = CreateSection("RoomsSection", content.transform, out roomsSectionHeader);
        pendingSection = CreateSection("PendingSection", content.transform, out pendingSectionHeader);

        clientRowPrefab = CreateRowPrefab("ClientRowPrefab",
            new[] { "Id", "Name", "Room", "State" });
        roomRowPrefab = CreateRowPrefab("RoomRowPrefab",
            new[] { "Id", "Name", "Players", "Map" });
        pendingRowPrefab = CreateRowPrefab("PendingRowPrefab",
            new[] { "P1", "P2", "Ready / Age" });

        panelRoot.SetActive(true);
        _visible = true;
    }

    private Transform CreateSection(string name, Transform parent, out Text headerText)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, 0);

        var headerBg = new GameObject("HeaderBg");
        headerBg.transform.SetParent(go.transform, false);
        var hbRt = headerBg.AddComponent<RectTransform>();
        hbRt.anchorMin = new Vector2(0, 1);
        hbRt.anchorMax = new Vector2(1, 1);
        hbRt.pivot = new Vector2(0.5f, 1);
        hbRt.offsetMin = new Vector2(0, -22);
        hbRt.offsetMax = new Vector2(0, 0);
        var hbImg = headerBg.AddComponent<Image>();
        hbImg.color = new Color(1, 1, 1, 0.06f);

        headerText = CreateLabel($"{name}Header", go.transform,
            new Vector2(6, -22), new Vector2(-6, -2), 12, FontStyle.Bold, new Color(1, 1, 1, 0.75f));
        headerText.text = name.Replace("Section", "").ToUpperInvariant();

        return go.transform;
    }

    private static void UpdateSectionHeader(Text header, string label, int count)
    {
        if (header != null)
        {
            header.text = $"{label}  ({count})";
        }
    }

    private Text CreateStatLabel(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        return CreateLabel(name, parent, offsetMin, offsetMax, 13, FontStyle.Normal, color);
    }

    private Text CreateLabel(
        string name,
        Transform parent,
        Vector2 offsetMin,
        Vector2 offsetMax,
        int fontSize,
        FontStyle style,
        Color color)
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
        rt.sizeDelta = new Vector2(0, 22);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.03f);

        float[] weights = columnHeaders.Length switch
        {
            4 => new[] { 0.12f, 0.48f, 0.22f, 0.18f },
            3 => new[] { 0.22f, 0.52f, 0.26f },
            _ => Enumerable.Repeat(1f / columnHeaders.Length, columnHeaders.Length).ToArray(),
        };

        float x = 0;
        for (int i = 0; i < columnHeaders.Length; i++)
        {
            float w = weights[i];
            var cell = new GameObject($"Cell{i}");
            cell.transform.SetParent(go.transform, false);
            var cellRt = cell.AddComponent<RectTransform>();
            cellRt.anchorMin = new Vector2(x, 0);
            cellRt.anchorMax = new Vector2(x + w, 1);
            cellRt.offsetMin = new Vector2(3, 2);
            cellRt.offsetMax = new Vector2(-3, -2);

            var cellText = cell.AddComponent<Text>();
            cellText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cellText.fontSize = 11;
            cellText.color = new Color(1, 1, 1, 0.85f);
            cellText.alignment = TextAnchor.MiddleLeft;
            cellText.horizontalOverflow = HorizontalWrapMode.Wrap;
            x += w;
        }

        go.SetActive(false);
        return go;
    }

    private void OnDestroy()
    {
        if (panelRoot != null)
        {
            Destroy(panelRoot);
        }
    }
}
