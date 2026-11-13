using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Jotunn.Managers;

using PortalNetwork.Core;
using PortalNetwork.Map;
using PortalNetwork.Portals;

using UnityEngine;
using UnityEngine.UI;

namespace PortalNetwork.UI;

/// <summary>
/// A list of reachable portals shown on the left of the large map while the picker is open.
/// Each row has a favourite star, the portal's name and biome (click to teleport) and its distance.
/// The portal the player is standing at comes first (not clickable), then favourites, then the rest.
/// Above the list: a "go back" button for the last trip.
/// </summary>
internal static class PortalPickerPanel
{
    private const float PanelWidth = 380f;
    private const float PanelHeight = 600f;
    private const float PanelMarginLeft = 40f;
    private const float RowHeight = 34f;
    private const float StarSize = 26f;
    private const float DistanceWidth = 64f;
    private const int Padding = 16;
    /// <summary>Used if the crafting list cannot be found; its value at the time of writing.</summary>
    private const float FallbackScrollSensitivity = 3000f;

    private static readonly Color StarOn = new(1f, 0.8f, 0.2f, 1f);
    private static readonly Color StarOff = new(1f, 1f, 1f, 0.25f);
    private static readonly Color ListBackground = new(0f, 0f, 0f, 0.35f);

    private static GameObject _panel;
    private static Button _goBackButton;
    private static Text _goBackText;
    private static RectTransform _content;
    private static ScrollRect _scroll;
    private static Text _emptyText;
    private static Sprite _starSprite;

    private static readonly List<PortalInfo> Portals = [];
    private static readonly List<GameObject> Rows = [];
    private static Vector3 _origin;
    private static ZDOID _current;

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

        _scroll.scrollSensitivity = VanillaScrollSensitivity();
        _panel.transform.SetAsLastSibling(); // above the map image so it receives the clicks
        _panel.SetActive(true);
        Rebuild();
        _scroll.verticalNormalizedPosition = 1f;
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

        CreateList(_panel.transform);
        return true;
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

        HashSet<string> favourites = PortalFavourites.GetFavourites();
        IEnumerable<(PortalInfo Portal, bool Current, bool Favourite, string Label)> rows = Portals
            .Select(p => (Portal: p, Current: p.Id == _current,
                Favourite: favourites.Contains(PortalFavourites.Key(p.Position)), Label: Label(p)))
            .OrderByDescending(r => r.Current)
            .ThenByDescending(r => r.Favourite)
            .ThenBy(r => r.Label, System.StringComparer.CurrentCultureIgnoreCase);

        foreach ((PortalInfo portal, bool current, bool favourite, string label) in rows)
        {
            Rows.Add(CreateRow(portal, current, favourite, label));
        }

        _emptyText.gameObject.SetActive(Rows.Count == 0);
        _emptyText.text = Localize("$portalnetwork_no_other_portals");
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

        GameObject row = new("Row", typeof(RectTransform));
        row.transform.SetParent(_content, false);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        SetControl(layout);
        AddLayout(row, height: RowHeight);

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

        return row;
    }

    private static void ClearRows()
    {
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
    private static string BiomeName(Vector3 position)
    {
        if (WorldGenerator.instance == null)
        {
            return "";
        }

        Heightmap.Biome biome = WorldGenerator.instance.GetBiome(position);
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

    /// <summary>A white five-pointed star drawn at runtime (tinted per row), so no asset bundle is needed.</summary>
    private static Sprite StarSprite()
    {
        if (_starSprite != null)
        {
            return _starSprite;
        }

        const int size = 64;
        const int samples = 4;
        Vector2 centre = new(size / 2f, size / 2f);
        Vector2[] points = new Vector2[10];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = (90f + (i * 36f)) * Mathf.Deg2Rad;
            float radius = i % 2 == 0 ? size * 0.48f : size * 0.2f;
            points[i] = centre + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

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

        _starSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        _starSprite.hideFlags = HideFlags.HideAndDontSave;
        return _starSprite;
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
