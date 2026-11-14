using System.Collections.Generic;

using PortalNetwork.Portals;

using UnityEngine;
using UnityEngine.UI;

namespace PortalNetwork.Map;

/// <summary>
/// Per-frame styling for our portal pins, in the configured colours: a tint (another for the home portal), a ring for
/// favourites, drawn above every other pin, and (in the picker) other pins dimmed.
/// Vanilla UpdatePins only runs when pins change, and then resets colours, so everything here sets absolute values.
/// </summary>
internal static class PortalPinStyle
{
    internal static Color PortalColor => PortalNetworkPlugin._portalIconColour.Value;

    internal static Color HomeColor => PortalNetworkPlugin._homeIconColour.Value;

    /// <summary>The favourite star's colour, also used for the ring around favourite portals' icons.</summary>
    internal static Color FavouriteColor => PortalNetworkPlugin._favouriteRingColour.Value;

    private const string RingName = "PortalNetwork_FavouriteRing";

    /// <summary>How far the ring extends past each side of the icon, as a fraction of the icon's size.</summary>
    private static float RingMargin => PortalNetworkPlugin._favouriteRingSize.Value;

    /// <summary>The ring's thickness, as a fraction of the ring image's size.</summary>
    private static float RingThickness => PortalNetworkPlugin._favouriteRingThickness.Value;

    private static Sprite _ringSprite;

    private static readonly List<Transform> Icons = [];
    private static readonly List<Transform> Names = [];

    /// <summary>
    /// Tints the given pins, with the home colour for <paramref name="home"/>, gives favourite portals a ring, and keeps
    /// their icons and labels drawn in front of all other pins.
    /// </summary>
    internal static void Apply(IEnumerable<Minimap.PinData> pins, Minimap.PinData home)
    {
        Icons.Clear();
        Names.Clear();
        HashSet<string> favourites = PortalFavourites.GetFavourites();
        foreach (Minimap.PinData pin in pins)
        {
            if (pin.m_iconElement != null)
            {
                pin.m_iconElement.color = pin == home ? HomeColor : PortalColor;

                // Our pins sit exactly on their portal, and favourites are stored by portal position.
                SetBorder(pin.m_iconElement, favourites.Contains(PortalFavourites.Key(pin.m_pos)));
            }
            if (pin.m_uiElement != null)
            {
                Icons.Add(pin.m_uiElement);
            }
            if (pin.m_NamePinData?.PinNameRectTransform != null)
            {
                Names.Add(pin.m_NamePinData.PinNameRectTransform);
            }
        }

        BringToFront(Icons);
        BringToFront(Names);
    }

    /// <summary>
    /// A gold ring around the icon, as a child image slightly larger than it. The ring is hollow and sits just outside
    /// the round portal icon, so it can be drawn on top without covering it. Vanilla recreates pin icons (e.g. when
    /// switching between the minimap and the large map), so this is checked every frame.
    /// </summary>
    private static void SetBorder(Image icon, bool favourite)
    {
        Transform ring = icon.transform.Find(RingName);
        if (ring == null)
        {
            if (!favourite)
            {
                return;
            }

            ring = CreateRing(icon.transform);
        }

        if (ring.gameObject.activeSelf != favourite)
        {
            ring.gameObject.SetActive(favourite);
        }
    }

    private static RectTransform CreateRing(Transform icon)
    {
        GameObject ring = new(RingName, typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)ring.transform;
        rect.SetParent(icon, false);
        rect.anchorMin = new Vector2(-RingMargin, -RingMargin);
        rect.anchorMax = new Vector2(1f + RingMargin, 1f + RingMargin);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = ring.GetComponent<Image>();
        image.sprite = RingSprite();
        image.color = FavouriteColor;
        image.raycastTarget = false;
        return rect;
    }

    /// <summary>A white, antialiased ring touching the edges of a transparent square, drawn once at runtime.</summary>
    private static Sprite RingSprite()
    {
        if (_ringSprite != null)
        {
            return _ringSprite;
        }

        const int size = 128;
        const float outer = size / 2f;
        float inner = outer - (size * RingThickness);
        Vector2 centre = new(size / 2f, size / 2f);

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
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);

                // Coverage of this pixel by the ring, with a one-pixel soft edge on both sides.
                float coverage = Mathf.Clamp01(Mathf.Min(distance - inner, outer - distance) + 0.5f);
                pixels[(y * size) + x] = new Color32(255, 255, 255, (byte)(255 * coverage));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        _ringSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        _ringSprite.hideFlags = HideFlags.HideAndDontSave;
        return _ringSprite;
    }

    /// <summary>Fades every pin that is not one of ours, so portal pins stand out on crowded maps.</summary>
    internal static void DimOthers(HashSet<Minimap.PinData> ours, float opacity)
    {
        Minimap minimap = Minimap.instance;
        if (minimap == null)
        {
            return;
        }

        // Same base colours as Minimap.UpdatePins, with alpha scaled.
        Color shared = new(0.7f, 0.7f, 0.7f, 0.8f * minimap.m_sharedMapDataFade * opacity);
        Color own = new(1f, 1f, 1f, opacity);

        foreach (Minimap.PinData pin in minimap.m_pins)
        {
            if (pin.m_iconElement == null || ours.Contains(pin))
            {
                continue;
            }

            Color color = pin.m_ownerID != 0L ? shared : own;
            pin.m_iconElement.color = color;
            if (pin.m_NamePinData?.PinNameText != null)
            {
                pin.m_NamePinData.PinNameText.color = color;
            }
        }
    }

    /// <summary>
    /// UI siblings draw in order, so our elements need to be the last children of their parent.
    /// Only reorders when something else has been added after them, to avoid dirtying the canvas every frame.
    /// </summary>
    private static void BringToFront(List<Transform> elements)
    {
        if (elements.Count == 0)
        {
            return;
        }

        Transform parent = elements[0].parent;
        if (parent == null)
        {
            return;
        }

        int firstFrontIndex = parent.childCount - elements.Count;
        bool inFront = true;
        foreach (Transform element in elements)
        {
            if (element.parent != parent || element.GetSiblingIndex() < firstFrontIndex)
            {
                inFront = false;
                break;
            }
        }

        if (inFront)
        {
            return;
        }

        foreach (Transform element in elements)
        {
            element.SetAsLastSibling();
        }
    }
}
