using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Jotunn.Managers;

using PortalNetwork.Core;
using PortalNetwork.Map;
using PortalNetwork.Network;
using PortalNetwork.Portals;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PortalNetwork.UI;

/// <summary>
/// A list of reachable portals shown on the left of the large map while the picker is open.
/// Each row has a favourite star (a green house for the home portal), the portal's name and biome (click to teleport),
/// its distance, a rename button and a name lock. The home portal comes first, then the portal the player is standing
/// at (not clickable), then favourites, then the rest.
/// Above the list: a "go back" button for the last trip, a search box that filters the list by name or biome, and
/// Biome and Distance buttons that split the list into sections (see <see cref="PortalGroups"/>). Inside each section,
/// as in the plain list: the portal the player is standing at, then favourites, then the rest by name. The home portal
/// stays at the very top.
/// </summary>
internal static class PortalPickerPanel
{
    private const float PanelWidth = 440f;
    private const float PanelHeight = 600f;
    private const float PanelMarginLeft = 40f;
    private const float RowHeight = 34f;
    private const float StarSize = 26f;
    private const float DistanceWidth = 64f;
    private const float IconButtonSize = 22f;
    private const int Padding = 16;
    /// <summary>Used if the crafting list cannot be found; its value at the time of writing.</summary>
    private const float FallbackScrollSensitivity = 3000f;

    private static Color StarOn => PortalPinStyle.FavouriteColor;
    private static readonly Color StarOff = new(1f, 1f, 1f, 0.25f);
    private static readonly Color ListBackground = new(0f, 0f, 0f, 0.35f);
    private static readonly Color IconIdle = new(1f, 1f, 1f, 0.6f);
    private static readonly Color LockOff = new(1f, 1f, 1f, 0.2f);
    private static Color RowHighlight => new(StarOn.r, StarOn.g, StarOn.b, 0.25f);

    private static GameObject _panel;
    private static Button _goBackButton;
    private static Text _goBackText;
    private static RectTransform _content;
    private static ScrollRect _scroll;
    private static Text _emptyText;
    private static Sprite _starSprite;
    private static Sprite _homeSprite;
    private static Sprite _pencilSprite;
    private static Sprite _lockSprite;
    private static InputField _search;
    private static Text _biomeButtonText;
    private static Text _distanceButtonText;

    private static readonly List<PortalInfo> Portals = [];
    private static readonly List<GameObject> Rows = [];

    /// <summary>Each portal row's background, lit while that portal is highlighted.</summary>
    private static readonly Dictionary<ZDOID, Image> RowBackgrounds = [];
    private static ZDOID _shownHighlight = ZDOID.None;
    private static Vector3 _origin;
    private static ZDOID _current;

    /// <summary>True while the search box has the keyboard, so the map and the portal keys leave the typing alone.</summary>
    internal static bool SearchFocused => _search != null && _search.isFocused && _panel != null && _panel.activeInHierarchy;

    internal static bool IsPointerOver()
    {
        return _panel != null
            && _panel.activeInHierarchy
            && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)_panel.transform, ZInput.pointerPosition, null);
    }

    /// <summary>Fills the list; <paramref name="portals"/> includes the one at <paramref name="current"/>.</summary>
    internal static void Show(IEnumerable<PortalInfo> portals, ZDOID current, Vector3 origin)
    {
        Minimap minimap = Minimap.instance;
        if (minimap == null || minimap.m_largeRoot == null)
        {
            return;
        }

        if (_panel == null && !Create(minimap.m_largeRoot.transform))
        {
            return;
        }

        Portals.Clear();
        Portals.AddRange(portals);
        _origin = origin;
        _current = current;

        _search.SetTextWithoutNotify("");
        _scroll.scrollSensitivity = VanillaScrollSensitivity();
        _panel.transform.SetAsLastSibling(); // above the map image so it receives the clicks
        _panel.SetActive(true);
        Rebuild();
        _scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>Moves the home portal (or none) and redraws the list, keeping the scroll position.</summary>
    internal static void SetHome(ZDOID home)
    {
        if (_panel == null || !_panel.activeSelf)
        {
            return;
        }

        for (int i = 0; i < Portals.Count; i++)
        {
            PortalInfo portal = Portals[i];
            portal.IsHome = portal.Id == home;
            Portals[i] = portal;
        }
        Rebuild();
    }

    /// <summary>Shows a portal's new name or name lock, keeping the scroll position and the search.</summary>
    internal static void UpdatePortal(PortalInfo changed)
    {
        if (_panel == null || !_panel.activeSelf)
        {
            return;
        }

        for (int i = 0; i < Portals.Count; i++)
        {
            if (Portals[i].Id == changed.Id)
            {
                changed.IsHome = Portals[i].IsHome;
                Portals[i] = changed;
            }
        }
        Rebuild();
    }

    internal static void Hide()
    {
        Portals.Clear();
        ClearRows();
        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    /// <summary>
    /// The scroll speed of the crafting list, so the wheel moves this list as fast as vanilla lists.
    /// </summary>
    private static float VanillaScrollSensitivity()
    {
        InventoryGui inventory = InventoryGui.instance;
        if (inventory == null || inventory.m_recipeEnsureVisible == null)
        {
            return FallbackScrollSensitivity;
        }

        ScrollRect vanilla = inventory.m_recipeEnsureVisible.GetComponent<ScrollRect>();
        return vanilla != null ? vanilla.scrollSensitivity : FallbackScrollSensitivity;
    }

    // ------------------------------------------------------------------ building

    private static bool Create(Transform parent)
    {
        GUIManager gui = GUIManager.Instance;
        if (gui == null || gui.AveriaSerifBold == null)
        {
            PortalNetworkPlugin._log.LogWarning("Jotunn GUI resources not ready, not showing the portal list.");
            return false;
        }

        _panel = gui.CreateWoodpanel(parent, Vector2.zero, Vector2.zero, Vector2.zero, 0f, 0f, false);
        _panel.name = "PortalNetwork_PortalList";
        RectTransform rect = (RectTransform)_panel.transform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rect.anchoredPosition = new Vector2(PanelMarginLeft, 0f);

        VerticalLayoutGroup layout = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(Padding, Padding, Padding, Padding);
        layout.spacing = 8f;
        SetControl(layout);

        Text title = CreateText(_panel.transform, "$portalnetwork_panel_title", gui.NorseBold, 30, gui.ValheimOrange, TextAnchor.MiddleCenter);
        AddLayout(title.gameObject, height: 36f);

        GameObject goBack = gui.CreateButton("", _panel.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        AddLayout(goBack, height: 38f);
        _goBackButton = goBack.GetComponent<Button>();
        _goBackText = goBack.GetComponentInChildren<Text>();
        _goBackText.horizontalOverflow = HorizontalWrapMode.Wrap;

        CreateSearch(_panel.transform);
        CreateGroupButtons(_panel.transform);
        CreateList(_panel.transform);
        return true;
    }

    /// <summary>Two buttons side by side that turn grouping by biome and by distance on and off; both are remembered.</summary>
    private static void CreateGroupButtons(Transform parent)
    {
        GameObject row = new("Grouping", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        SetControl(layout);
        layout.childForceExpandWidth = true;
        AddLayout(row, height: 32f);

        _biomeButtonText = CreateGroupButton(row.transform, "$portalnetwork_group_biome", PortalNetworkPlugin._groupByBiome);
        _distanceButtonText = CreateGroupButton(row.transform, "$portalnetwork_group_distance", PortalNetworkPlugin._groupByDistance);
    }

    private static Text CreateGroupButton(Transform row, string label, BepInEx.Configuration.ConfigEntry<bool> setting)
    {
        GameObject button = GUIManager.Instance.CreateButton(Localize(label), row, Vector2.zero, Vector2.zero, Vector2.zero);
        AddLayout(button, height: 30f, flexibleWidth: 1f);
        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            setting.Value = !setting.Value;
            Rebuild();
            _scroll.verticalNormalizedPosition = 1f;
        });
        return button.GetComponentInChildren<Text>();
    }

    /// <summary>Lit (orange) while on, plain while off.</summary>
    private static void UpdateGroupButtons()
    {
        _biomeButtonText.color = PortalNetworkPlugin._groupByBiome.Value ? GUIManager.Instance.ValheimOrange : GUIManager.Instance.ValheimBeige;
        _distanceButtonText.color = PortalNetworkPlugin._groupByDistance.Value ? GUIManager.Instance.ValheimOrange : GUIManager.Instance.ValheimBeige;
    }

    private static void CreateSearch(Transform parent)
    {
        GUIManager gui = GUIManager.Instance;
        GameObject search = gui.CreateInputField(parent, Vector2.zero, Vector2.zero, Vector2.zero,
            InputField.ContentType.Standard, Localize("$portalnetwork_search_placeholder"), 16);
        search.name = "Search";
        AddLayout(search, height: 32f);
        _search = search.GetComponent<InputField>();
        _search.characterLimit = 40;
        ClearButton.Add(_search);
        _search.onValueChanged.AddListener(_ =>
        {
            Rebuild();
            _scroll.verticalNormalizedPosition = 1f;
        });
    }

    private static void CreateList(Transform parent)
    {
        GameObject scrollObject = new("List", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);
        scrollObject.GetComponent<Image>().color = ListBackground;
        AddLayout(scrollObject, flexibleHeight: 1f);

        GameObject viewport = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = (RectTransform)viewport.transform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(4f, 4f);
        viewportRect.offsetMax = new Vector2(-16f, -4f); // room for the scrollbar

        GameObject content = new("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        _content = (RectTransform)content.transform;
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 4f;
        contentLayout.padding = new RectOffset(4, 4, 4, 4);
        SetControl(contentLayout);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject scrollbarObject = DefaultControls.CreateScrollbar(GUIManager.Instance.ValheimControlResources);
        scrollbarObject.transform.SetParent(scrollObject.transform, false);
        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        GUIManager.Instance.ApplyScrollbarStyle(scrollbar);
        RectTransform scrollbarRect = (RectTransform)scrollbarObject.transform;
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(10f, -8f);
        scrollbarRect.anchoredPosition = new Vector2(-3f, 0f);

        _scroll = scrollObject.GetComponent<ScrollRect>();
        _scroll.viewport = viewportRect;
        _scroll.content = _content;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.verticalScrollbar = scrollbar;
        _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        _emptyText = CreateText(content.transform, "", GUIManager.Instance.AveriaSerifBold, 16, GUIManager.Instance.ValheimBeige, TextAnchor.MiddleCenter);
        _emptyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        AddLayout(_emptyText.gameObject, height: 60f);
    }

    // ------------------------------------------------------------------ contents

    private static void Rebuild()
    {
        ClearRows();
        if (_panel == null)
        {
            return;
        }

        UpdateGoBack();
        UpdateGroupButtons();

        HashSet<string> favourites = PortalFavourites.GetFavourites();
        string filter = _search != null ? _search.text.Trim() : "";
        List<RowData> rows = [.. Portals
            .Select(p => new RowData(p, p.Id == _current, favourites.Contains(PortalFavourites.Key(p.Position)), Label(p)))
            .Where(r => filter.Length == 0 || r.Label.IndexOf(filter, System.StringComparison.CurrentCultureIgnoreCase) >= 0)];

        // The home portal stays at the very top, whatever the grouping.
        foreach (RowData home in rows.Where(r => r.Portal.IsHome))
        {
            Rows.Add(CreateRow(home.Portal, home.Current, home.Favourite, home.Label));
        }
        List<RowData> rest = [.. rows.Where(r => !r.Portal.IsHome)];

        bool byBiome = PortalNetworkPlugin._groupByBiome.Value;
        bool byDistance = PortalNetworkPlugin._groupByDistance.Value;
        if (!byBiome && !byDistance)
        {
            AddRows(rest);
        }
        else
        {
            foreach (PortalGroups.Group<RowData> group in PortalGroups.Build(rest, r => r.Portal.Position, BiomeAt,
                byBiome, byDistance, _origin, PortalNetworkPlugin._groupDistance.Value))
            {
                Rows.Add(CreateHeader(GroupTitle(group, byBiome, byDistance)));
                AddRows(group.Items);
            }
        }

        _emptyText.gameObject.SetActive(Rows.Count == 0);
        _emptyText.text = Localize(filter.Length > 0 ? "$portalnetwork_no_matches" : "$portalnetwork_no_other_portals");
    }

    /// <summary>One row's portal and how it is shown.</summary>
    private readonly struct RowData(PortalInfo portal, bool current, bool favourite, string label)
    {
        public readonly PortalInfo Portal = portal;
        public readonly bool Current = current;
        public readonly bool Favourite = favourite;
        public readonly string Label = label;
    }

    /// <summary>The same order as the plain list: the portal the player is standing at, then favourites, then by name.</summary>
    private static void AddRows(IEnumerable<RowData> rows)
    {
        IEnumerable<RowData> ordered = rows
            .OrderByDescending(r => r.Current)
            .ThenByDescending(r => r.Favourite)
            .ThenBy(r => r.Label, System.StringComparer.CurrentCultureIgnoreCase);
        foreach (RowData row in ordered)
        {
            Rows.Add(CreateRow(row.Portal, row.Current, row.Favourite, row.Label));
        }
    }

    /// <summary>
    /// "Mistlands", "2.1 km east", or "Mistlands · 2.1 km east". The section with the portal the player is standing at
    /// is "Around you".
    /// </summary>
    private static string GroupTitle(PortalGroups.Group<RowData> group, bool byBiome, bool byDistance)
    {
        string biome = group.Biome == Heightmap.Biome.None
            ? Localize("$portalnetwork_group_other")
            : Localize("$biome_" + group.Biome.ToString().ToLowerInvariant());
        if (!byDistance)
        {
            return biome;
        }

        string place = group.Items.Any(r => r.Current)
            ? Localize("$portalnetwork_group_here")
            : FormatDistance(Utils.DistanceXZ(_origin, group.Centre)) + " " + Localize(PortalGroups.DirectionToken(_origin, group.Centre));
        return byBiome ? biome + " · " + place : place;
    }

    private static GameObject CreateHeader(string title)
    {
        GUIManager gui = GUIManager.Instance;
        Text header = CreateText(_content, title, gui.AveriaSerifBold, 16, gui.ValheimOrange, TextAnchor.LowerLeft);
        header.text = title;
        AddLayout(header.gameObject, height: 26f);
        return header.gameObject;
    }

    private static void UpdateGoBack()
    {
        _goBackButton.onClick.RemoveAllListeners();

        PortalInfo target = default;
        bool found = PortalFavourites.TryGetGoBackKey(_origin, out string key)
            && TryFind(key, out target);

        _goBackButton.interactable = found;
        if (found)
        {
            _goBackText.text = Localize("$portalnetwork_go_back") + " " + Label(target);
            _goBackButton.onClick.AddListener(() => PortalMapPicker.Teleport(target));
        }
        else
        {
            _goBackText.text = Localize("$portalnetwork_no_last_portal");
        }
    }

    private static bool TryFind(string key, out PortalInfo portal)
    {
        foreach (PortalInfo candidate in Portals)
        {
            if (candidate.Id != _current && PortalFavourites.Key(candidate.Position) == key)
            {
                portal = candidate;
                return true;
            }
        }

        portal = default;
        return false;
    }

    private static GameObject CreateRow(PortalInfo portal, bool current, bool favourite, string label)
    {
        GUIManager gui = GUIManager.Instance;

        // A see-through background so the whole row, gaps included, counts as under the mouse.
        GameObject row = new("Row", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(_content, false);
        Image background = row.GetComponent<Image>();
        background.color = Color.clear;
        RowBackgrounds[portal.Id] = background;
        row.AddComponent<RowHover>().Portal = portal.Id;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        SetControl(layout);
        AddLayout(row, height: RowHeight);

        if (portal.IsHome)
        {
            // Home portal: a green house instead of the favourite star, since it is always at the top anyway.
            GameObject home = new("Home", typeof(RectTransform), typeof(Image));
            home.transform.SetParent(row.transform, false);
            AddLayout(home, width: StarSize, height: StarSize);
            Image homeImage = home.GetComponent<Image>();
            homeImage.sprite = HomeSprite();
            homeImage.preserveAspect = true;
            homeImage.color = PortalPinStyle.HomeColor;
            homeImage.raycastTarget = false;
        }
        else
        {
            // Favourite star
            GameObject star = new("Star", typeof(RectTransform), typeof(Image), typeof(Button));
            star.transform.SetParent(row.transform, false);
            AddLayout(star, width: StarSize, height: StarSize);
            Image starImage = star.GetComponent<Image>();
            starImage.sprite = StarSprite();
            starImage.preserveAspect = true;
            starImage.color = favourite ? StarOn : StarOff;
            Button starButton = star.GetComponent<Button>();
            starButton.transition = Selectable.Transition.None;
            starButton.onClick.AddListener(() =>
            {
                PortalFavourites.SetFavourite(portal, !favourite);
                Rebuild(); // re-sort so favourites move to the top straight away
            });
        }

        // Name and biome: click to teleport (the portal you are standing at is shown but disabled)
        GameObject name = gui.CreateButton(label, row.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        AddLayout(name, height: RowHeight - 4f, flexibleWidth: 1f);
        Text nameText = name.GetComponentInChildren<Text>();
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameText.resizeTextForBestFit = true;
        nameText.resizeTextMinSize = 11;
        nameText.resizeTextMaxSize = 16;
        RectTransform nameTextRect = (RectTransform)nameText.transform;
        nameTextRect.anchorMin = Vector2.zero;
        nameTextRect.anchorMax = Vector2.one;
        nameTextRect.offsetMin = new Vector2(10f, 0f);
        nameTextRect.offsetMax = new Vector2(-6f, 0f);
        Button nameButton = name.GetComponent<Button>();
        nameButton.interactable = !current;
        if (!current)
        {
            nameButton.onClick.AddListener(() => PortalMapPicker.Teleport(portal));
        }

        // Distance
        string distanceText = current
            ? Localize("$portalnetwork_here")
            : FormatDistance(Vector3.Distance(_origin, portal.Position));
        Text distance = CreateText(row.transform, distanceText,
            gui.AveriaSerifBold, 14, gui.ValheimBeige, TextAnchor.MiddleRight);
        AddLayout(distance.gameObject, width: DistanceWidth, height: RowHeight);

        AddRenameButton(row.transform, portal);
        AddLockButton(row.transform, portal);
        return row;
    }

    /// <summary>While the mouse is over a portal's row, that portal's icon stands out on the map.</summary>
    private sealed class RowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public ZDOID Portal;

        public void OnPointerEnter(PointerEventData eventData)
        {
            PortalMapPicker.Highlight(Portal);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PortalMapPicker.Unhighlight(Portal);
        }

        /// <summary>Rows are rebuilt (sorting, search, grouping) and hidden with the list, without a pointer exit.</summary>
        private void OnDisable()
        {
            PortalMapPicker.Unhighlight(Portal);
        }
    }

    /// <summary>The player as the server sees them, for showing only the buttons they may use (the server checks again).</summary>
    private static Requester LocalRequester()
    {
        Player local = Player.m_localPlayer;
        return new Requester(local != null ? local.GetPlayerID() : 0L, PlayerIdentity.LocalIsAdmin());
    }

    /// <summary>A pencil that opens the game's naming box. Hidden if the name is locked and the player may not unlock it.</summary>
    private static void AddRenameButton(Transform row, PortalInfo portal)
    {
        if (!PortalRegistry.CanRename(portal, LocalRequester()))
        {
            AddSpacer(row);
            return;
        }

        Button button = CreateIconButton(row, "Rename", PencilSprite(), IconIdle);
        button.onClick.AddListener(() => TextInput.instance.RequestText(new RenameReceiver(portal), "$piece_portal_tag", PortalRegistry.MaxNameLength));
    }

    /// <summary>
    /// A padlock: gold while the name is locked. The builder and admins click it to lock or unlock the name; anyone else
    /// only sees it while the name is locked.
    /// </summary>
    private static void AddLockButton(Transform row, PortalInfo portal)
    {
        bool canChange = PortalRegistry.CanChange(portal, LocalRequester());
        if (!canChange && !portal.NameLocked)
        {
            AddSpacer(row);
            return;
        }

        Button button = CreateIconButton(row, "NameLock", LockSprite(), portal.NameLocked ? StarOn : LockOff);
        button.interactable = canChange;
        if (canChange)
        {
            button.onClick.AddListener(() => PortalNetworkRpc.SendNameLockRequest(portal.Id, !portal.NameLocked));
        }
    }

    private static Button CreateIconButton(Transform row, string name, Sprite sprite, Color color)
    {
        GameObject icon = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        icon.transform.SetParent(row, false);
        AddLayout(icon, width: IconButtonSize, height: IconButtonSize);
        Image image = icon.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = color;
        Button button = icon.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        return button;
    }

    /// <summary>Keeps the rows lined up when a button isn't shown.</summary>
    private static void AddSpacer(Transform row)
    {
        GameObject spacer = new("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(row, false);
        AddLayout(spacer, width: IconButtonSize, height: IconButtonSize);
    }

    /// <summary>What the game's naming box edits: starts with the portal's name, and asks the server to rename it.</summary>
    private sealed class RenameReceiver(PortalInfo portal) : TextReceiver
    {
        public string GetText()
        {
            return portal.Name ?? "";
        }

        public void SetText(string text)
        {
            PortalNetworkRpc.SendRenameRequest(portal.Id, text);
        }
    }

    /// <summary>
    /// Lights the row of the highlighted portal, the one under the mouse in the list or on the map, so the list and the
    /// map can be matched up. Called every frame; only changes the rows when the highlight moves.
    /// </summary>
    internal static void ShowHighlight(ZDOID portal)
    {
        if (portal == _shownHighlight)
        {
            return;
        }

        if (RowBackgrounds.TryGetValue(_shownHighlight, out Image previous) && previous != null)
        {
            previous.color = Color.clear;
        }
        if (RowBackgrounds.TryGetValue(portal, out Image next) && next != null)
        {
            next.color = RowHighlight;
        }
        _shownHighlight = portal;
    }

    private static void ClearRows()
    {
        RowBackgrounds.Clear();
        _shownHighlight = ZDOID.None; // the new rows are lit on the next frame
        foreach (GameObject row in Rows)
        {
            if (row != null)
            {
                row.SetActive(false);
                Object.Destroy(row);
            }
        }
        Rows.Clear();
    }

    /// <summary>"Home (Plains)", or "Home (Plains, private)".</summary>
    internal static string Label(PortalInfo portal)
    {
        string details = BiomeName(portal.Position);
        if (portal.Visibility == PortalVisibility.Private)
        {
            string suffix = Localize("$portalnetwork_private_suffix");
            details = string.IsNullOrEmpty(details) ? suffix : details + ", " + suffix;
        }

        string name = PortalNames.Display(portal);
        return string.IsNullOrEmpty(details) ? name : name + " (" + details + ")";
    }

    /// <summary>Biome from world generation, so it works for portals far outside the loaded area.</summary>
    private static Heightmap.Biome BiomeAt(Vector3 position)
    {
        return WorldGenerator.instance != null ? WorldGenerator.instance.GetBiome(position) : Heightmap.Biome.None;
    }

    private static string BiomeName(Vector3 position)
    {
        if (WorldGenerator.instance == null)
        {
            return "";
        }

        Heightmap.Biome biome = BiomeAt(position);
        return biome == Heightmap.Biome.None
            ? ""
            : Localize("$biome_" + biome.ToString().ToLowerInvariant());
    }

    private static string FormatDistance(float metres)
    {
        return metres < 1000f
            ? Mathf.RoundToInt(metres).ToString(CultureInfo.InvariantCulture) + " m"
            : (metres / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + " km";
    }

    // ------------------------------------------------------------------ helpers

    private static Text CreateText(Transform parent, string text, Font font, int size, Color color, TextAnchor alignment)
    {
        GameObject textObject = new("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text component = textObject.GetComponent<Text>();
        GUIManager.Instance.ApplyTextStyle(component, font, color, size, true);
        component.alignment = alignment;
        component.text = Localize(text);
        component.raycastTarget = false;
        return component;
    }

    private static void SetControl(HorizontalOrVerticalLayoutGroup layout)
    {
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = layout is VerticalLayoutGroup;
        layout.childForceExpandHeight = false;
    }

    private static void AddLayout(GameObject target, float width = -1f, float height = -1f,
        float flexibleWidth = -1f, float flexibleHeight = -1f)
    {
        if (!target.TryGetComponent(out LayoutElement element))
        {
            element = target.AddComponent<LayoutElement>();
        }

        element.minWidth = width;
        element.preferredWidth = width;
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleWidth = flexibleWidth;
        element.flexibleHeight = flexibleHeight;
    }

    private static string Localize(string text)
    {
        return string.IsNullOrEmpty(text) ? text : PortalNetworkLocalization.Localize(text);
    }

    private const int SpriteSize = 64;

    /// <summary>A white five-pointed star drawn at runtime (tinted per row), so no asset bundle is needed.</summary>
    private static Sprite StarSprite()
    {
        if (_starSprite != null)
        {
            return _starSprite;
        }

        Vector2 centre = new(SpriteSize / 2f, SpriteSize / 2f);
        Vector2[] points = new Vector2[10];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = (90f + (i * 36f)) * Mathf.Deg2Rad;
            float radius = i % 2 == 0 ? SpriteSize * 0.48f : SpriteSize * 0.2f;
            points[i] = centre + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        _starSprite = PolygonSprite(points);
        return _starSprite;
    }

    /// <summary>A white house (roof and walls, with a door cut out), drawn at runtime like the star.</summary>
    private static Sprite HomeSprite()
    {
        if (_homeSprite != null)
        {
            return _homeSprite;
        }

        // Outline in 0..1 units, counter-clockwise from the bottom-left corner of the walls.
        Vector2[] outline =
        [
            new(0.18f, 0.04f), new(0.42f, 0.04f), new(0.42f, 0.32f), new(0.58f, 0.32f), new(0.58f, 0.04f),
            new(0.82f, 0.04f), new(0.82f, 0.5f), new(0.96f, 0.5f), new(0.5f, 0.94f), new(0.04f, 0.5f),
            new(0.18f, 0.5f),
        ];
        Vector2[] points = new Vector2[outline.Length];
        for (int i = 0; i < outline.Length; i++)
        {
            points[i] = outline[i] * SpriteSize;
        }

        _homeSprite = PolygonSprite(points);
        return _homeSprite;
    }

    /// <summary>A white pencil, pointing down to the left, drawn at runtime like the star.</summary>
    private static Sprite PencilSprite()
    {
        if (_pencilSprite != null)
        {
            return _pencilSprite;
        }

        // Body from the tip (bottom-left) up to the end (top-right), in 0..1 units, counter-clockwise.
        Vector2[] outline =
        [
            new(0.06f, 0.06f), new(0.32f, 0.14f), new(0.94f, 0.76f), new(0.76f, 0.94f), new(0.14f, 0.32f),
        ];
        _pencilSprite = PolygonSprite(Scale(outline));
        return _pencilSprite;
    }

    /// <summary>A white padlock (body and shackle as one outline), drawn at runtime like the star.</summary>
    private static Sprite LockSprite()
    {
        if (_lockSprite != null)
        {
            return _lockSprite;
        }

        // Counter-clockwise from the body's bottom-left corner, round the shackle and back down its inside.
        Vector2[] outline =
        [
            new(0.16f, 0.04f), new(0.84f, 0.04f), new(0.84f, 0.52f), new(0.74f, 0.52f), new(0.74f, 0.72f),
            new(0.66f, 0.88f), new(0.5f, 0.94f), new(0.34f, 0.88f), new(0.26f, 0.72f), new(0.26f, 0.52f),
            new(0.38f, 0.52f), new(0.38f, 0.7f), new(0.43f, 0.79f), new(0.5f, 0.82f), new(0.57f, 0.79f),
            new(0.62f, 0.7f), new(0.62f, 0.52f), new(0.16f, 0.52f),
        ];
        _lockSprite = PolygonSprite(Scale(outline));
        return _lockSprite;
    }

    private static Vector2[] Scale(Vector2[] outline)
    {
        Vector2[] points = new Vector2[outline.Length];
        for (int i = 0; i < outline.Length; i++)
        {
            points[i] = outline[i] * SpriteSize;
        }
        return points;
    }

    /// <summary>A white, antialiased filled polygon on a transparent square texture.</summary>
    private static Sprite PolygonSprite(Vector2[] points)
    {
        const int size = SpriteSize;
        const int samples = 4;

        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        Vector2 p = new(x + ((sx + 0.5f) / samples), y + ((sy + 0.5f) / samples));
                        if (InPolygon(p, points))
                        {
                            inside++;
                        }
                    }
                }
                byte alpha = (byte)(255 * inside / (samples * samples));
                pixels[(y * size) + x] = new Color32(255, 255, 255, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static bool InPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y)
                && point.x < ((polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y)) + polygon[i].x)
            {
                inside = !inside;
            }
        }
        return inside;
    }
}
