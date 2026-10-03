using System.Collections.Generic;
using HarmonyLib;

namespace PortalNetwork
{
    /// <summary>
    /// Vanilla only lights up a portal when it has a tag-linked partner. PortalNetwork
    /// teleports to any portal, so every placed portal should look (and read) as active.
    /// </summary>
    internal static class PortalVisualPatches
    {
        private static readonly HashSet<string> Logged = new HashSet<string>();

        // Placement ghosts have no ZDO; leave vanilla behaviour alone for those.
        internal static bool IsPlacedPortal(TeleportWorld portal)
        {
            ZNetView nview = portal.GetComponent<ZNetView>();
            return nview != null && nview.GetZDO() != null;
        }
    }

    // Private methods are patched by name (string) rather than nameof(): the Publicizer
    // is not applied in CI, so nameof() on a private member would fail to compile there.

    // Hover text ("connected"/"unconnected") and the vanilla glow logic read HaveTarget().
    [HarmonyPatch(typeof(TeleportWorld), "HaveTarget")]
    internal static class TeleportWorld_HaveTarget_Patch
    {
        private static bool Prefix(TeleportWorld __instance, ref bool __result)
        {
            if (!PortalVisualPatches.IsPlacedPortal(__instance)) return true;

            __result = true;
            return false; // skip the original
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), "TargetFound")]
    internal static class TeleportWorld_TargetFound_Patch
    {
        private static bool Prefix(TeleportWorld __instance, ref bool __result)
        {
            if (!PortalVisualPatches.IsPlacedPortal(__instance)) return true;

            __result = true;
            return false;
        }
    }

    // Direct glow: after vanilla's Update has set the emission colour, overwrite it with
    // the "target found" colour. Uses only public fields, so it doesn't depend on the
    // private methods above being patched successfully.
    [HarmonyPatch(typeof(TeleportWorld), "Update")]
    internal static class TeleportWorld_Update_Patch
    {
        private static void Postfix(TeleportWorld __instance)
        {
            if (__instance.m_model == null || !PortalVisualPatches.IsPlacedPortal(__instance)) return;

            __instance.m_model.material.SetColor("_EmissionColor", __instance.m_colorTargetfound);
        }
    }

    // Direct swirl: vanilla only shows it when a linked target exists. Re-apply the same
    // condition vanilla uses (player nearby and able to teleport) without the link check.
    [HarmonyPatch(typeof(TeleportWorld), "UpdatePortal")]
    internal static class TeleportWorld_UpdatePortal_Patch
    {
        private static void Postfix(TeleportWorld __instance)
        {
            if (__instance.m_proximityRoot == null || __instance.m_target_found == null) return;
            if (!PortalVisualPatches.IsPlacedPortal(__instance)) return;

            Player closest = Player.GetClosestPlayer(__instance.m_proximityRoot.position, __instance.m_activationRange);
            bool active = closest != null && closest.IsTeleportable(__instance.m_allowAllItems);
            __instance.m_target_found.SetActive(active);
        }
    }
}
