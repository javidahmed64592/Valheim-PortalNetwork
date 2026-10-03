using System;
using HarmonyLib;

namespace PortalNetwork
{
    internal static class PortalOwnership
    {
        internal const string SetVisibilityRpc = "RPC_PortalNetwork_SetVisibility";
    }

    // Register our RPC on every placed portal. Same pattern vanilla uses for RPC_SetTag.
    [HarmonyPatch(typeof(TeleportWorld), "Awake")]
    internal static class TeleportWorld_Awake_Patch
    {
        private static void Postfix(TeleportWorld __instance)
        {
            ZNetView nview = __instance.GetComponent<ZNetView>();
            if (nview == null || nview.GetZDO() == null) return; // placement ghost

            nview.Register<int>(PortalOwnership.SetVisibilityRpc, (sender, value) => OnSetVisibility(nview, sender, value));
        }

        // Runs on the ZDO's owner. It only accepts instructions that came from the server,
        // which has already checked that the requesting player is allowed to make the change.
        private static void OnSetVisibility(ZNetView nview, long sender, int value)
        {
            if (sender != ZRoutedRpc.instance.GetServerPeerID()) return;
            if (!nview.IsValid() || !nview.IsOwner()) return;
            if (!Enum.IsDefined(typeof(PortalVisibility), value)) return;

            nview.GetZDO().Set(PortalRegistry.VisibilityHash, value);
        }
    }

    // Shift + Use asks the server to toggle visibility; plain Use keeps vanilla's tag-naming behaviour.
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
    internal static class TeleportWorld_Interact_Patch
    {
        private static bool Prefix(TeleportWorld __instance, Humanoid human, bool hold, bool alt, ref bool __result)
        {
            if (!alt || hold) return true;

            ZNetView nview = __instance.GetComponent<ZNetView>();
            ZDO zdo = nview != null ? nview.GetZDO() : null;
            if (zdo == null || !(human is Player)) return true;

            __result = true;
            PortalInfo info = PortalRegistry.ToInfo(zdo);
            PortalVisibility next = info.Visibility == PortalVisibility.Public
                ? PortalVisibility.Private
                : PortalVisibility.Public;

            // The server decides, and tells the player the outcome.
            PortalNetworkRpc.SendVisibilityRequest(zdo.m_uid, next);
            return false;
        }
    }

    // Append the current visibility (and the toggle hint, for the builder) to the hover text.
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
    internal static class TeleportWorld_GetHoverText_Patch
    {
        private static void Postfix(TeleportWorld __instance, ref string __result)
        {
            ZNetView nview = __instance.GetComponent<ZNetView>();
            ZDO zdo = nview != null ? nview.GetZDO() : null;
            if (zdo == null) return;

            PortalInfo info = PortalRegistry.ToInfo(zdo);
            string text = "\nVisibility: " + (info.Visibility == PortalVisibility.Public ? "Public" : "Private");

            // Cosmetic only: the server makes the real decision when the toggle is pressed.
            Player local = Player.m_localPlayer;
            if (local != null && PortalRegistry.CanChange(info, local.GetPlayerID()))
            {
                text += "\n[<color=yellow><b>Shift + Use</b></color>] Toggle visibility";
            }

            __result += text;
        }
    }
}
