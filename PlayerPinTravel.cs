using System.Collections.Generic;
using System.Runtime.CompilerServices;

using HarmonyLib;

using UnityEngine;

namespace PortalNetwork;

/// <summary>
/// Vanilla slides other players' map icons towards their new position at a fixed 200 m/s, so the icon of a player who
/// teleported far away crawls across the map for many seconds. When a player's position jumps (a teleport), this makes
/// the icon take the configured number of seconds to get there instead, however far it is. Normal movement is left
/// to vanilla.
/// </summary>
[HarmonyPatch(typeof(Minimap), "UpdatePlayerPins")]
internal static class Minimap_UpdatePlayerPins_Patch
{
    /// <summary>
    /// Player positions are only sent every 2 seconds, so a fast ship moves a few tens of metres between updates.
    /// A bigger jump than this is treated as a teleport.
    /// </summary>
    private const float TeleportDistance = 100f;

    private sealed class PinTravel
    {
        public Vector3 Target;

        /// <summary>Speed for the current teleport, or 0 when the pin is left to vanilla.</summary>
        public float Speed;
    }

    private static readonly ConditionalWeakTable<Minimap.PinData, PinTravel> Travels = new();
    private static readonly List<Minimap.PinData> PinsBefore = [];
    private static readonly List<Vector3> PositionsBefore = [];
    private static readonly List<string> NamesBefore = [];

    private static void Prefix(Minimap __instance)
    {
        PinsBefore.Clear();
        PositionsBefore.Clear();
        NamesBefore.Clear();
        foreach (Minimap.PinData pin in __instance.m_playerPins)
        {
            PinsBefore.Add(pin);
            PositionsBefore.Add(pin.m_pos);
            NamesBefore.Add(pin.m_name);
        }
    }

    private static void Postfix(Minimap __instance, float dt)
    {
        List<Minimap.PinData> pins = __instance.m_playerPins;
        List<ZNet.PlayerInfo> players = __instance.m_tempPlayerInfo;
        float seconds = PortalNetworkPlugin._playerIconTeleportSeconds.Value;
        for (int i = 0; i < pins.Count && i < players.Count; i++)
        {
            Minimap.PinData pin = pins[i];
            Vector3 target = players[i].m_position;

            // Vanilla only slides a pin that already showed this player; new or reassigned pins snap into place.
            bool slid = i < PinsBefore.Count && PinsBefore[i] == pin && NamesBefore[i] == players[i].m_name;
            if (!slid)
            {
                _ = Travels.Remove(pin);
                Travels.Add(pin, new PinTravel { Target = target });
                continue;
            }

            PinTravel travel = Travels.GetValue(pin, _ => new PinTravel { Target = target });
            Vector3 from = PositionsBefore[i];
            if (Vector3.Distance(target, travel.Target) > TeleportDistance)
            {
                travel.Speed = seconds > 0f ? Vector3.Distance(from, target) / seconds : float.PositiveInfinity;
            }

            travel.Target = target;
            if (travel.Speed <= 0f)
            {
                continue;
            }

            Vector3 position = float.IsPositiveInfinity(travel.Speed)
                ? target
                : Vector3.MoveTowards(from, target, travel.Speed * dt);
            if (position == target)
            {
                travel.Speed = 0f;
            }

            if (position != pin.m_pos)
            {
                pin.m_pos = position;
                __instance.m_pinUpdateRequired = true;
            }
        }
    }
}
