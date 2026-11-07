using System.Collections.Generic;

using UnityEngine;

namespace PortalNetwork;

internal static class PortalMapPicker
{
    private sealed class Entry
    {
        public Minimap.PinData Pin;
        public PortalInfo Portal;
    }

    private const float MinClickRadiusPixels = 24f;
    private const float MaxDistanceFromPortal = 2f;
    private static Vector3 _origin;
    private static readonly List<Entry> Entries = [];
    private static readonly HashSet<Minimap.PinData> PickerPins = [];

    internal static bool IsActive => Entries.Count > 0;

    internal static void Open(List<PortalInfo> portals, ZDOID enteredPortal, Vector3 enteredPosition)
    {
        Minimap minimap = Minimap.instance;
        if (minimap == null)
        {
            return;
        }

        Close(); // clear anything left over from a previous open
        PortalMapOverlay.Clear();

        foreach (PortalInfo portal in portals)
        {
            if (portal.Id == enteredPortal)
            {
                continue; // no point listing the portal you are standing at
            }

            string label = PortalNames.Display(portal);
            if (portal.Visibility == PortalVisibility.Private)
            {
                label += " (" + PortalNetworkLocalization.Localize("$portalnetwork_private_suffix") + ")";
            }

            // save: false keeps these out of the character's saved map data.
            Minimap.PinData pin = minimap.AddPin(portal.Position, Minimap.PinType.Icon4, label, false, false);
            pin.m_doubleSize = true;
            Entries.Add(new Entry { Pin = pin, Portal = portal });
            _ = PickerPins.Add(pin);
        }

        if (Entries.Count == 0)
        {
            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$portalnetwork_no_other_portals");
            }
            return;
        }

        _origin = enteredPosition;

        minimap.ShowPointOnMap(enteredPosition);
        if (!Minimap.IsOpen())
        {
            minimap.SetMapMode(Minimap.MapMode.Large);
        }

        List<PortalInfo> listed = [];
        foreach (Entry entry in Entries)
        {
            listed.Add(entry.Portal);
        }
        PortalPickerPanel.Show(listed, enteredPosition);
    }

    /// <summary>Called from the Minimap.OnMapLeftClick patch while the picker is active.</summary>
    internal static void HandleClick()
    {
        Vector2 pointer = ZInput.pointerPosition;
        Entry best = null;
        float bestDistance = float.MaxValue;

        foreach (Entry entry in Entries)
        {
            RectTransform rect = entry.Pin.m_uiElement;
            if (rect == null || !rect.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            float radius = Mathf.Max(MinClickRadiusPixels, rect.rect.width * rect.lossyScale.x);
            float distance = Vector2.Distance(screen, pointer);

            if (distance < radius && distance < bestDistance)
            {
                best = entry;
                bestDistance = distance;
            }
        }

        if (best == null)
        {
            return;
        }

        Teleport(best.Portal);
    }

    /// <summary>Used by both the map pins and the portal list.</summary>
    internal static void Teleport(PortalInfo destination)
    {
        if (!IsActive)
        {
            return;
        }

        Minimap.instance.SetMapMode(Minimap.MapMode.Small); // closes the map; the patch clears our pins
        PortalNetworkClient.RequestTeleport(destination);
    }

    internal static void Close()
    {
        PortalPickerPanel.Hide();
        if (Entries.Count == 0)
        {
            return;
        }

        Minimap minimap = Minimap.instance;
        if (minimap != null)
        {
            foreach (Entry entry in Entries)
            {
                minimap.RemovePin(entry.Pin);
            }
        }
        Entries.Clear();
        PickerPins.Clear(); // RemovePin triggers a vanilla pin refresh, which restores the dimmed pins' colours
    }

    /// <summary>Called every frame (from the Minimap.Update patch) while the picker is open.</summary>
    internal static void Tick()
    {
        if (!IsActive)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (player == null
            || player.IsDead()
            || Vector3.Distance(player.transform.position, _origin) > MaxDistanceFromPortal)
        {
            CloseMap();
            return;
        }

        PortalPinStyle.Apply(PickerPins);
        PortalPinStyle.DimOthers(PickerPins, PortalNetworkPlugin._otherPinOpacity.Value);
    }

    private static void CloseMap()
    {
        // The SetMapMode patch clears our pins when the large map closes.
        Minimap.instance.SetMapMode(Minimap.MapMode.Small);
    }
}
