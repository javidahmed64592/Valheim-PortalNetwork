using HarmonyLib;

namespace PortalNetwork;

[HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
internal static class TeleportWorld_Teleport_Patch
{
    /// <summary>
    /// Returning false skips vanilla's tag-linked teleport entirely.
    /// </summary>
    private static bool Prefix(TeleportWorld __instance, Player player)
    {
        ZNetView nview = __instance.GetComponent<ZNetView>();
        ZDO zdo = nview != null ? nview.GetZDO() : null;
        if (zdo == null)
        {
            return true;
        }

        if (!PortalNetworkClient.CanUsePortals(player, __instance))
        {
            return false;
        }

        PortalNetworkClient.RequestPortalList(zdo.m_uid, zdo.GetPosition());
        return false;
    }
}