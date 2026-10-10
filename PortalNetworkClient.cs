using System.Collections.Generic;
using UnityEngine;

namespace PortalNetwork
{
    internal static class PortalNetworkClient
    {
        private const float ExitDistance = 1f; // vanilla default for m_exitDistance
        private const float RequestCooldownSeconds = 1f;

        private static ZDOID _enteredPortal = ZDOID.None;
        private static Vector3 _enteredPosition;
        private static float _lastRequestTime = -10f;

        // One entry per outstanding list request (true = map overlay, false = portal picker), oldest first.
        private static readonly Queue<bool> PendingRequests = new Queue<bool>();

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

        internal static void RequestPortalList(ZDOID enteredPortal, Vector3 enteredPosition, Player player)
        {
            if (Time.unscaledTime - _lastRequestTime < RequestCooldownSeconds) return;
            _lastRequestTime = Time.unscaledTime;

            _enteredPortal = enteredPortal;
            _enteredPosition = enteredPosition;
            PendingRequests.Enqueue(false);
            PortalNetworkRpc.SendListRequest();
        }

        internal static void RequestOverlayList()
        {
            PendingRequests.Enqueue(true);
            PortalNetworkRpc.SendListRequest();
        }

        internal static void OnPortalListReceived(List<PortalInfo> portals)
        {
            bool forOverlay = PendingRequests.Count > 0 && PendingRequests.Dequeue();
            if (Player.m_localPlayer == null) return;

            PortalNetworkPlugin.Log.LogDebug($"Client: received {portals.Count} portal(s)");
            if (forOverlay)
            {
                PortalMapOverlay.Show(portals);
                return;
            }
            PortalMapPicker.Open(portals, _enteredPortal, _enteredPosition);
        }

        /// <summary>The player clicked a pin. The server confirms the portal is still there and visible.</summary>
        internal static void RequestTeleport(PortalInfo destination)
        {
            PortalNetworkRpc.SendTeleportRequest(destination.Id);
        }

        internal static void OnTeleportApproved(string name, Vector3 position, Quaternion rotation)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return;

            Vector3 forward = rotation * Vector3.forward;
            Vector3 target = position + forward * ExitDistance + Vector3.up;

            PortalNetworkPlugin.Log.LogDebug($"Teleporting to '{name}' at {position}");
            player.TeleportTo(target, rotation, distantTeleport: true);
            Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed);
        }

        internal static void ShowMessage(string text)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return;

            player.Message(MessageHud.MessageType.Center, text);
        }
    }
}
