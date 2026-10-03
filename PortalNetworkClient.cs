using System.Collections.Generic;
using UnityEngine;

namespace PortalNetwork
{
    internal static class PortalNetworkClient
    {
        private const float ExitDistance = 1f; // vanilla default for m_exitDistance
        private const float RequestCooldownSeconds = 1f;

        private static ZDOID _enteredPortal = ZDOID.None;
        private static float _lastRequestTime = -10f;

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

        internal static void RequestPortalList(ZDOID enteredPortal, Player player)
        {
            if (Time.unscaledTime - _lastRequestTime < RequestCooldownSeconds) return;
            _lastRequestTime = Time.unscaledTime;

            _enteredPortal = enteredPortal;
            PortalNetworkRpc.SendListRequest(player.GetPlayerID());
        }

        internal static void OnPortalListReceived(List<PortalInfo> portals)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return;

            PortalNetworkPlugin.Log.LogInfo($"Client: received {portals.Count} portal(s)");
            foreach (PortalInfo p in portals)
            {
                PortalNetworkPlugin.Log.LogInfo($"  '{p.Name}' at {p.Position} ({p.Visibility})");
            }

            // TEMPORARY test behaviour: jump to the next portal in the list, cycling.
            // The map UI replaces this in the next step.
            if (portals.Count < 2)
            {
                player.Message(MessageHud.MessageType.Center, "No other portals available");
                return;
            }

            int current = portals.FindIndex(p => p.Id == _enteredPortal);
            PortalInfo destination = portals[(current + 1) % portals.Count];
            TeleportTo(player, destination);
        }

        internal static void TeleportTo(Player player, PortalInfo destination)
        {
            Vector3 forward = destination.Rotation * Vector3.forward;
            Vector3 position = destination.Position + forward * ExitDistance + Vector3.up;

            PortalNetworkPlugin.Log.LogInfo($"Teleporting to '{destination.Name}' at {destination.Position}");
            player.TeleportTo(position, destination.Rotation, distantTeleport: true);
            Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed);
        }
    }
}
