using System.Collections.Generic;

using PortalNetwork.Home;
using PortalNetwork.Map;
using PortalNetwork.Portals;

using UnityEngine;

namespace PortalNetwork.Network;

internal static class PortalNetworkClient
{
    /// <summary>
    /// vanilla default for m_exitDistance
    /// </summary>
    private const float ExitDistance = 1f;
    private const float RequestCooldownSeconds = 1f;

    private static ZDOID _enteredPortal = ZDOID.None;
    private static Vector3 _enteredPosition;
    private static float _lastRequestTime = -10f;

    /// <summary>What a portal list was asked for.</summary>
    private enum ListPurpose
    {
        /// <summary>Walking into a portal: open the portal map.</summary>
        Picker = 0,

        /// <summary>The portal icons on the normal map.</summary>
        Overlay = 1,

        /// <summary>The go back key at a portal: find the last trip's portal and travel there.</summary>
        GoBack = 2,
    }

    /// <summary>
    /// One entry per outstanding list request, oldest first.
    /// </summary>
    private static readonly Queue<ListPurpose> PendingRequests = new();

    /// <summary>Same restrictions vanilla applies in TeleportWorld.Teleport.</summary>
    internal static bool CanUsePortals(Player player, TeleportWorld portal)
    {
        if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_blocked");
            return false;
        }

        if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBossPortals)
            && (RandEventSystem.instance.GetBossEvent() != null
                || (ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses, out float value) && value > 0f)))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_blockedbyboss");
            return false;
        }

        if (!player.IsTeleportable(portal.m_allowAllItems))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return false;
        }

        return true;
    }

    internal static void RequestPortalList(ZDOID enteredPortal, Vector3 enteredPosition)
    {
        if (Time.unscaledTime - _lastRequestTime < RequestCooldownSeconds)
        {
            return;
        }

        _lastRequestTime = Time.unscaledTime;

        _enteredPortal = enteredPortal;
        _enteredPosition = enteredPosition;
        PendingRequests.Enqueue(ListPurpose.Picker);
        PortalNetworkRpc.SendListRequest();
    }

    /// <summary>
    /// The player pressed the go back key at the portal <paramref name="fromPortal"/>. The last trip only remembers
    /// where the portals stand, so this asks for the portals this player can see, then travels to the one there.
    /// </summary>
    internal static void RequestGoBack(ZDOID fromPortal, Vector3 fromPosition)
    {
        if (Time.unscaledTime - _lastRequestTime < RequestCooldownSeconds)
        {
            return;
        }

        _lastRequestTime = Time.unscaledTime;

        _enteredPortal = fromPortal;
        _enteredPosition = fromPosition;
        PendingRequests.Enqueue(ListPurpose.GoBack);
        PortalNetworkRpc.SendListRequest();
    }

    /// <summary>The player pressed the home key at the portal <paramref name="fromPortal"/>.</summary>
    internal static void RequestHomeTeleport(ZDOID fromPortal, Vector3 fromPosition)
    {
        if (Time.unscaledTime - _lastRequestTime < RequestCooldownSeconds)
        {
            return;
        }

        _lastRequestTime = Time.unscaledTime;

        _enteredPortal = fromPortal;
        _enteredPosition = fromPosition;
        PortalNetworkRpc.SendHomeTeleportRequest(fromPortal);
    }

    internal static void RequestOverlayList()
    {
        PendingRequests.Enqueue(ListPurpose.Overlay);
        PortalNetworkRpc.SendListRequest();
    }

    internal static void OnPortalListReceived(List<PortalInfo> portals)
    {
        ListPurpose purpose = PendingRequests.Count > 0 ? PendingRequests.Dequeue() : ListPurpose.Picker;
        if (Player.m_localPlayer == null)
        {
            return;
        }

        PortalNetworkPlugin._log.LogDebug($"Client: received {portals.Count} portal(s)");
        HomePortal.OnListReceived(portals);
        switch (purpose)
        {
            case ListPurpose.Overlay:
                PortalMapOverlay.Show(portals);
                break;
            case ListPurpose.GoBack:
                if (TryFindGoBack(portals, _enteredPortal, _enteredPosition, out PortalInfo target))
                {
                    RequestTeleport(target);
                }
                else
                {
                    ShowMessage("$portalnetwork_no_last_portal");
                }
                break;
            default:
                PortalMapPicker.Open(portals, _enteredPortal, _enteredPosition);
                break;
        }
    }

    /// <summary>
    /// The portal "go back" leads to from the portal <paramref name="current"/> at <paramref name="currentPosition"/>,
    /// out of the portals this player can see. False if there was no last trip or its portal is gone or hidden.
    /// </summary>
    internal static bool TryFindGoBack(IEnumerable<PortalInfo> portals, ZDOID current, Vector3 currentPosition, out PortalInfo target)
    {
        target = default;
        if (!PortalFavourites.TryGetGoBackKey(currentPosition, out string key))
        {
            return false;
        }

        foreach (PortalInfo portal in portals)
        {
            if (portal.Id != current && PortalFavourites.Key(portal.Position) == key)
            {
                target = portal;
                return true;
            }
        }
        return false;
    }

    /// <summary>The player clicked a pin. The server confirms the portal is still there and visible.</summary>
    internal static void RequestTeleport(PortalInfo destination)
    {
        PortalNetworkRpc.SendTeleportRequest(destination.Id);
    }

    internal static void OnTeleportApproved(string name, Vector3 position, Quaternion rotation)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        Vector3 forward = rotation * Vector3.forward;
        Vector3 target = position + (forward * ExitDistance) + Vector3.up;

        PortalFavourites.RecordTrip(_enteredPosition, position);

        PortalNetworkPlugin._log.LogDebug($"Teleporting to '{name}' at {position}");
        _ = player.TeleportTo(target, rotation, distantTeleport: true);
        Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed);
    }

    internal static void ShowMessage(string text)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        player.Message(MessageHud.MessageType.Center, text);
    }
}
