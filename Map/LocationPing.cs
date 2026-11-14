namespace PortalNetwork.Map;

/// <summary>
/// Pings the player's own location on everyone's map with a key, without opening the map. This sends the same ping
/// as clicking your position on the map.
/// </summary>
internal static class LocationPing
{
    /// <summary>Called every frame (from the Minimap.Update patch).</summary>
    internal static void Tick()
    {
        Player player = Player.m_localPlayer;
        if (player == null || Chat.instance == null || !PortalNetworkPlugin._pingKey.Value.IsDown())
        {
            return;
        }

        // TakeInput is false while the map, a menu, the inventory, chat or a text box is open, and while dead or
        // teleporting.
        if (!player.TakeInput())
        {
            return;
        }

        Chat.instance.SendPing(player.transform.position);
    }
}
