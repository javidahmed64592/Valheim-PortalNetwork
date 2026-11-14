using System.Collections.Generic;

using PortalNetwork.Core;
using PortalNetwork.Home;
using PortalNetwork.Network;
using PortalNetwork.Portals;
using PortalNetwork.UI;

using UnityEngine;

namespace PortalNetwork.Map;

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
    private static Entry _home;
    private static ZDOID _entered = ZDOID.None;

    /// <summary>True if the travel home key would do something: there is a home portal and we are not at it.</summary>
    internal static bool CanTravelHome => IsActive && _home != null;

    /// <summary>True if the set home key would do something for the local player.</summary>
    internal static bool CanSetHome => IsActive && !_entered.IsNone() && HomePortal.LocalPlayerCanSetHome();

    /// <summary>True if the portal the player is standing at is the home portal.</summary>
    internal static bool AtHome { get; private set; }

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
                AtHome = portal.IsHome;
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
            Entry entry = new() { Pin = pin, Portal = portal };
            Entries.Add(entry);
            if (portal.IsHome)
            {
                _home = entry;
            }

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
        _entered = enteredPortal;

        minimap.ShowPointOnMap(enteredPosition);
        if (!Minimap.IsOpen())
        {
            minimap.SetMapMode(Minimap.MapMode.Large);
        }

        PortalPickerPanel.Show(portals, enteredPortal, enteredPosition);
    }

    /// <summary>Called from the Minimap.OnMapLeftClick patch while the picker is active.</summary>
    internal static void HandleClick()
    {
        PickAt(ZInput.pointerPosition);
    }

    /// <summary>
    /// With a gamepad there is no pointer: the map is moved under a crosshair in the middle of the screen,
    /// and the confirm button picks the portal under it.
    /// </summary>
    internal static void HandleGamepadConfirm()
    {
        PickAt(new Vector2(Screen.width / 2f, Screen.height / 2f));
    }

    private static void PickAt(Vector2 pointer)
    {
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
        _home = null;
        _entered = ZDOID.None;
        AtHome = false;
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

        if (!Minimap.InTextInput())
        {
            if (CanSetHome && PortalNetworkPlugin._setHomeKey.Value.IsDown())
            {
                // The list and pins update when the server announces the new home portal.
                PortalNetworkRpc.SendSetHomeRequest(_entered, !AtHome);
            }
            else if (CanTravelHome && PortalNetworkPlugin._travelHomeKey.Value.IsDown())
            {
                Teleport(_home.Portal);
                return;
            }
        }

        PortalPinStyle.Apply(PickerPins, _home?.Pin);
        PortalPinStyle.DimOthers(PickerPins, PortalNetworkPlugin._otherPinOpacity.Value);
    }

    /// <summary>The server announced a new home portal (or none) while the picker may be open.</summary>
    internal static void OnHomeChanged(ZDOID home)
    {
        if (!IsActive)
        {
            return;
        }

        _home = null;
        foreach (Entry entry in Entries)
        {
            entry.Portal.IsHome = entry.Portal.Id == home;
            if (entry.Portal.IsHome)
            {
                _home = entry;
            }
        }
        AtHome = home == _entered;
        PortalPickerPanel.SetHome(home);
    }

    private static void CloseMap()
    {
        // The SetMapMode patch clears our pins when the large map closes.
        Minimap.instance.SetMapMode(Minimap.MapMode.Small);
    }
}
