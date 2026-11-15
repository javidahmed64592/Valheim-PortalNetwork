using HarmonyLib;

using PortalNetwork.UI;

namespace PortalNetwork.Patches;

/// <summary>
/// The game ignores the map's keys (M and Escape close it), the player's controls and chat while a map pin is being
/// named. Typing in the portal list's search box counts the same, so typing an M doesn't close the map.
/// </summary>
[HarmonyPatch(typeof(Minimap), nameof(Minimap.InTextInput))]
internal static class Minimap_InTextInput_Patch
{
    private static void Postfix(ref bool __result)
    {
        __result = __result || PortalPickerPanel.SearchFocused;
    }
}
