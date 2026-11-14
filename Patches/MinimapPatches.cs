using HarmonyLib;

using PortalNetwork.Home;
using PortalNetwork.Map;
using PortalNetwork.UI;

namespace PortalNetwork.Patches;

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
/// With a gamepad, the confirm button on the map opens the box for naming a new pin at the crosshair. While the
/// portal picker is open it picks the portal under the crosshair instead, and no pins are added.
/// </summary>
[HarmonyPatch(typeof(Minimap), "ShowPinNameInput")]
internal static class Minimap_ShowPinNameInput_Patch
{
    private static bool Prefix()
    {
        if (!PortalMapPicker.IsActive)
        {
            return true;
        }

        if (!ZInput.IsMouseActive())
        {
            PortalMapPicker.HandleGamepadConfirm();
        }
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
/// While the pointer is over the portal list, the scroll wheel scrolls the list instead of zooming the map.
/// </summary>
[HarmonyPatch(typeof(Minimap), "UpdateMap")]
internal static class Minimap_UpdateMap_Patch
{
    private static void Prefix(ref bool takeInput)
    {
        if (takeInput && PortalPickerPanel.IsPointerOver())
        {
            takeInput = false;
        }
    }
}

/// <summary>
/// Every frame: update the picker, the portal overlay and key hint, and check the ping and home portal keys.
/// </summary>
[HarmonyPatch(typeof(Minimap), "Update")]
internal static class Minimap_Update_Patch
{
    private static void Postfix()
    {
        PortalMapPicker.Tick();
        PortalMapOverlay.Tick();
        PortalKeyHint.Tick();
        LocationPing.Tick();
        HomePortal.Tick();
    }
}
