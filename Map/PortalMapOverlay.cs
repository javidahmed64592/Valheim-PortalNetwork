using System.Collections.Generic;

using PortalNetwork.Core;
using PortalNetwork.Network;
using PortalNetwork.Portals;

namespace PortalNetwork.Map;

/// <summary>
/// Optional portal icons on the normal map, toggled with a keybind. Unlike the picker, these pins are not clickable.
/// </summary>
internal static class PortalMapOverlay
{
    private static readonly List<Minimap.PinData> Pins = [];
    private static bool _enabled;
    private static bool _loaded;
    private static bool _requestPending;

    /// <summary>Called every frame (from the Minimap.Update patch).</summary>
    internal static void Tick()
    {
        if (PortalMapPicker.IsActive)
        {
            Clear(); // the picker has its own pins
            return;
        }

        if (Minimap.IsOpen() && !Minimap.InTextInput() && PortalNetworkPlugin._toggleKey.Value.IsDown())
        {
            _enabled = !_enabled;
            if (!_enabled)
            {
                Clear();
            }
        }

        if (_enabled && !_loaded && !_requestPending)
        {
            _requestPending = true;
            PortalNetworkClient.RequestOverlayList();
        }

        // Applies on both the minimap and the large map.
        PortalPinStyle.Apply(Pins);
    }

    internal static void Show(List<PortalInfo> portals)
    {
        _requestPending = false;

        Minimap minimap = Minimap.instance;
        if (minimap == null || !_enabled || PortalMapPicker.IsActive)
        {
            return;
        }

        Clear();
        _loaded = true;
        foreach (PortalInfo portal in portals)
        {
            string label = PortalNames.Display(portal);
            if (portal.Visibility == PortalVisibility.Private)
            {
                label += " (" + PortalNetworkLocalization.Localize("$portalnetwork_private_suffix") + ")";
            }

            // save: false keeps these out of the character's saved map data.
            Minimap.PinData pin = minimap.AddPin(portal.Position, Minimap.PinType.Icon4, label, false, false);
            pin.m_doubleSize = true;
            Pins.Add(pin);
        }
    }

    /// <summary>Removes the pins; the next Tick fetches them again if the overlay is still enabled.</summary>
    internal static void Clear()
    {
        _loaded = false;

        Minimap minimap = Minimap.instance;
        if (minimap != null)
        {
            foreach (Minimap.PinData pin in Pins)
            {
                minimap.RemovePin(pin);
            }
        }
        Pins.Clear();
    }
}
