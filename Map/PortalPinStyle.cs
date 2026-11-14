using System.Collections.Generic;

using PortalNetwork.Portals;

using UnityEngine;
using UnityEngine.UI;

namespace PortalNetwork.Map;

/// <summary>
/// Per-frame styling for our portal pins: red tint (green for the home portal), a gold border for favourites, drawn above every other pin, and (in the picker) other pins dimmed.
/// Vanilla UpdatePins only runs when pins change, and then resets colours, so everything here sets absolute values.
/// </summary>
internal static class PortalPinStyle
{
    internal static readonly Color HomeColor = new(0.3f, 0.85f, 0.3f, 1f);

    /// <summary>The favourite star's gold, also used for the border around favourite portals' icons.</summary>
    internal static readonly Color FavouriteColor = new(1f, 0.8f, 0.2f, 1f);

    /// <summary>How far the border sticks out from the icon, in the map's UI units.</summary>
    private static readonly Vector2 BorderDistance = new(2.5f, -2.5f);

    private static readonly List<Transform> Icons = [];
    private static readonly List<Transform> Names = [];

    /// <summary>
    /// Tints the given pins red, or green for <paramref name="home"/>, gives favourite portals a gold border, and keeps
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
                pin.m_iconElement.color = pin == home ? HomeColor : Color.red;

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
    /// A UI Outline draws the icon again in gold, offset in four directions behind it, so the border follows the
    /// portal icon's shape. Vanilla recreates pin icons (e.g. when switching between the minimap and the large map),
    /// so this is checked every frame.
    /// </summary>
    private static void SetBorder(Image icon, bool favourite)
    {
        if (!icon.TryGetComponent(out Outline outline))
        {
            if (!favourite)
            {
                return;
            }

            outline = icon.gameObject.AddComponent<Outline>();
            outline.effectColor = FavouriteColor;
            outline.effectDistance = BorderDistance;
            outline.useGraphicAlpha = true;
        }

        if (outline.enabled != favourite)
        {
            outline.enabled = favourite;
        }
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
