using PortalNetwork.Core;

using UnityEngine;

namespace PortalNetwork.Map;

/// <summary>
/// Pings the spot the player is looking at on everyone's map with a key, without opening the map. This sends the same
/// ping as clicking that spot on the map. Looking at the sky (or further than anything loaded) pings where the player
/// stands.
/// </summary>
internal static class LocationPing
{
    private const float MaxPingDistance = 1000f;

    /// <summary>The ground, buildings and trees the crosshair can land on. Creatures and water are looked through.</summary>
    private static int _rayMask;

    /// <summary>Called every frame (from the Minimap.Update patch).</summary>
    internal static void Tick()
    {
        Player player = Player.m_localPlayer;
        if (player == null || Chat.instance == null || !KeyInput.IsDown(PortalNetworkPlugin._pingKey.Value))
        {
            return;
        }

        // TakeInput is false while the map, a menu, the inventory, chat or a text box is open, and while dead or
        // teleporting.
        if (!player.TakeInput())
        {
            return;
        }

        Chat.instance.SendPing(LookedAtPoint(player));
    }

    private static Vector3 LookedAtPoint(Player player)
    {
        if (GameCamera.instance == null)
        {
            return player.transform.position;
        }

        if (_rayMask == 0)
        {
            _rayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");
        }

        Transform camera = GameCamera.instance.transform;
        return Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, MaxPingDistance, _rayMask, QueryTriggerInteraction.Ignore)
            ? hit.point
            : player.transform.position;
    }
}
