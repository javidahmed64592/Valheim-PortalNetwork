using System;
using HarmonyLib;

namespace PortalNetwork
{
    internal static class PortalOwnership
    {
        internal const string SetVisibilityRpc = "RPC_PortalNetwork_SetVisibility";

        /// <summary>The builder may change it. Portals with no recorded builder (creator 0) are open to anyone.</summary>
        internal static bool CanChange(PortalInfo info, Player player)
        {
            return info.OwnerId == 0L || info.OwnerId == player.GetPlayerID();
        }
    }

    // Register our RPC on every placed portal. Same pattern vanilla uses for RPC_SetTag.
    [HarmonyPatch(typeof(TeleportWorld), "Awake")]
    internal static class TeleportWorld_Awake_Patch
    {
        private static void Postfix(TeleportWorld __instance)
        {
            ZNetView nview = __instance.GetComponent<ZNetView>();
            if (nview == null || nview.GetZDO() == null) return; // placement ghost

            nview.Register<int>(PortalOwnership.SetVisibilityRpc, (sender, value) => OnSetVisibility(nview, value));
        }

        // Runs on the ZDO's owner; only the owner may write ZDO data.
        private static void OnSetVisibility(ZNetView nview, int value)
        {
            if (!nview.IsValid() || !nview.IsOwner()) return;
            if (!Enum.IsDefined(typeof(PortalVisibility), value)) return;

            nview.GetZDO().Set(PortalRegistry.VisibilityHash, value);
        }
    }

    // Shift + Use toggles visibility; plain Use keeps vanilla's tag-naming behaviour.
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
    internal static class TeleportWorld_Interact_Patch
    {
        private static bool Prefix(TeleportWorld __instance, Humanoid human, bool hold, bool alt, ref bool __result)
        {
            if (!alt || hold) return true;

            ZNetView nview = __instance.GetComponent<ZNetView>();
            ZDO zdo = nview != null ? nview.GetZDO() : null;
            Player player = human as Player;
            if (zdo == null || player == null) return true;

            __result = true;
            PortalInfo info = PortalRegistry.ToInfo(zdo);

            if (!PortalOwnership.CanChange(info, player))
            {
                human.Message(MessageHud.MessageType.Center, "Only the builder can change who sees this portal");
                return false;
            }

            PortalVisibility next = info.Visibility == PortalVisibility.Public
                ? PortalVisibility.Private
                : PortalVisibility.Public;

            nview.InvokeRPC(PortalOwnership.SetVisibilityRpc, (int)next);
            human.Message(
                MessageHud.MessageType.Center,
                next == PortalVisibility.Public ? "Portal visible to everyone" : "Portal visible only to you");
            return false;
        }
    }

    // Append the current visibility (and the toggle hint, for the builder) to the hover text.
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

            Player local = Player.m_localPlayer;
            if (local != null && PortalOwnership.CanChange(info, local))
            {
                text += "\n[<color=yellow><b>Shift + Use</b></color>] Toggle visibility";
            }

            __result += text;
        }
    }
}
