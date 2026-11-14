using System;
using System.Collections.Generic;

using BepInEx.Configuration;

using PortalNetwork.Network;
using PortalNetwork.Portals;

using UnityEngine;

namespace PortalNetwork.Home;

/// <summary>
/// The world's home portal, as seen by this client, and the keys used while looking at a portal: one travels to the
/// home portal, the other (admins and the host only) makes the portal the new home. The server stores the home portal
/// and makes every decision; this only remembers which portal it is for the hover text, the map and the portal list.
/// </summary>
internal static class HomePortal
{
    /// <summary>How often to ask the server for the home portal while looking at a portal.</summary>
    private const float RefreshSeconds = 5f;

    private static ZDOID _home = ZDOID.None;
    private static float _lastRequestTime = -100f;
    private static ZNet _world;

    internal static bool HasHome => !_home.IsNone();

    internal static bool IsHome(ZDOID portalId)
    {
        return HasHome && portalId == _home;
    }

    internal static void OnHomeReceived(ZDOID home)
    {
        _home = home;
    }

    /// <summary>Every list includes the home portal, since it is always public.</summary>
    internal static void OnListReceived(List<PortalInfo> portals)
    {
        _home = ZDOID.None;
        foreach (PortalInfo portal in portals)
        {
            if (portal.IsHome)
            {
                _home = portal.Id;
            }
        }
    }

    /// <summary>Called every frame (from the Minimap.Update patch).</summary>
    internal static void Tick()
    {
        Player player = Player.m_localPlayer;
        if (player == null || ZNet.instance == null)
        {
            return;
        }

        if (_world != ZNet.instance)
        {
            // A different world or server: forget the old home portal.
            _world = ZNet.instance;
            _home = ZDOID.None;
            _lastRequestTime = -100f;
        }

        TeleportWorld portal = HoveredPortal(player);
        ZNetView nview = portal != null ? portal.GetComponent<ZNetView>() : null;
        ZDO zdo = nview != null ? nview.GetZDO() : null;
        if (zdo == null)
        {
            return;
        }

        if (Time.unscaledTime - _lastRequestTime > RefreshSeconds)
        {
            _lastRequestTime = Time.unscaledTime;
            PortalNetworkRpc.SendHomeRequest();
        }

        // False while a menu, the inventory, chat or a text box is open, and while dead or teleporting.
        if (!player.TakeInput())
        {
            return;
        }

        if (PortalNetworkPlugin._setHomeKey.Value.IsDown())
        {
            PortalNetworkRpc.SendSetHomeRequest(zdo.m_uid);
        }
        else if (PortalNetworkPlugin._travelHomeKey.Value.IsDown()
            && !IsHome(zdo.m_uid)
            && PortalNetworkClient.CanUsePortals(player, portal))
        {
            PortalNetworkClient.RequestHomeTeleport(zdo.m_uid, zdo.GetPosition());
        }
    }

    /// <summary>"Shift + H": modifiers first, without "Left"/"Right", matching the vanilla "Shift + E" hints.</summary>
    internal static string KeyText(KeyboardShortcut shortcut)
    {
        List<string> parts = [];
        foreach (KeyCode modifier in shortcut.Modifiers)
        {
            parts.Add(KeyName(modifier));
        }
        parts.Add(KeyName(shortcut.MainKey));
        return string.Join(" + ", parts);
    }

    private static string KeyName(KeyCode key)
    {
        string name = key.ToString();
        foreach (string side in new[] { "Left", "Right" })
        {
            if (name.StartsWith(side, StringComparison.Ordinal) && name.Length > side.Length)
            {
                return name.Substring(side.Length);
            }
        }
        return name;
    }

    private static TeleportWorld HoveredPortal(Player player)
    {
        GameObject hovering = player.GetHoverObject();
        return hovering != null ? hovering.GetComponentInParent<TeleportWorld>() : null;
    }
}
