using HarmonyLib;

namespace PortalNetwork.Patches;

/// <summary>
/// The world save keeps every portal in one separate file, which vanilla only rewrites when a portal is built,
/// destroyed or linked to another. Other changes to a portal, such as its name, whether it is public and whether it
/// is the home portal, were lost when the world was reloaded, since this mod rarely links portals. Marking the portals
/// as changed before each save makes the file get rewritten every time. It is small, so this costs very little.
/// </summary>
[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.PrepareSave))]
internal static class ZDOMan_PrepareSave_Patch
{
    private static void Prefix(ZDOMan __instance)
    {
        // Vanilla skips the file when there are no portals, so don't write an empty one.
        if (__instance.m_portalObjects.Count > 0)
        {
            __instance.SetDirtyPortals();
        }
    }
}
