using HarmonyLib;

namespace PortalNetwork;

/// <summary>
/// While the portal picker is open, a left click picks a portal instead of toggling a pin.
/// </summary>
[HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
internal static class Minimap_OnMapLeftClick_Patch
{
    private static bool Prefix()
    {
        if (!PortalMapPicker.IsActive)
        {
            return true;
        }

        PortalMapPicker.HandleClick();
        return false; // skip vanilla
    }
}

/// <summary>
/// Whenever the large map closes (M, Esc, or after picking), remove the picker's pins.
/// </summary>
[HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
internal static class Minimap_SetMapMode_Patch
{
    private static void Postfix(Minimap.MapMode mode)
    {
        if (mode != Minimap.MapMode.Large)
        {
            PortalMapPicker.Close(); // overlay pins stay so they remain on the minimap
        }
    }
}

/// <summary>
/// Every frame: let the picker decide whether the player has walked away.
/// </summary>
[HarmonyPatch(typeof(Minimap), "Update")]
internal static class Minimap_Update_Patch
{
    private static void Postfix()
    {
        PortalMapPicker.Tick();
        PortalMapOverlay.Tick();
        PortalKeyHint.Tick();
    }
}
