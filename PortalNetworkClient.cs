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
            PortalNetworkRpc.SendListRequest(player.GetPlayerID());
        }

        internal static void OnPortalListReceived(List<PortalInfo> portals)
        {
            if (Player.m_localPlayer == null) return;

            PortalNetworkPlugin.Log.LogDebug($"Client: received {portals.Count} portal(s)");
            PortalMapPicker.Open(portals, _enteredPortal, _enteredPosition);
        }

        internal static void TeleportTo(Player player, PortalInfo destination)
        {
            Vector3 forward = destination.Rotation * Vector3.forward;
            Vector3 position = destination.Position + forward * ExitDistance + Vector3.up;

            PortalNetworkPlugin.Log.LogDebug($"Teleporting to '{destination.Name}' at {destination.Position}");
            player.TeleportTo(position, destination.Rotation, distantTeleport: true);
            Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed);
        }
    }
}
