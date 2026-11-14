using System;

using HarmonyLib;

using PortalNetwork.Core;
using PortalNetwork.Home;
using PortalNetwork.Map;
using PortalNetwork.Network;
using PortalNetwork.Portals;

using UnityEngine;

namespace PortalNetwork.Patches;

internal static class PortalOwnership
{
    internal const string SetVisibilityRpc = "RPC_PortalNetwork_SetVisibility";
    internal const string SetHomeRpc = "RPC_PortalNetwork_SetHome";
}

/// <summary>
/// Register our RPC on every placed portal. Same pattern vanilla uses for RPC_SetTag.
/// </summary>
[HarmonyPatch(typeof(TeleportWorld), "Awake")]
internal static class TeleportWorld_Awake_Patch
{
    private static void Postfix(TeleportWorld __instance)
    {
        ZNetView nview = __instance.GetComponent<ZNetView>();
        if (nview == null || nview.GetZDO() == null)
        {
            return; // placement ghost
        }

        nview.Register<int>(PortalOwnership.SetVisibilityRpc, (sender, value) => OnSetVisibility(nview, sender, value));
        nview.Register<long>(PortalOwnership.SetHomeRpc, (sender, stamp) => OnSetHome(nview, sender, stamp));
    }

    /// <summary>
    /// Runs on the ZDO's owner. It only accepts instructions that came from the server,
    /// which has already checked that the requesting player is allowed to make the change.
    /// </summary>
    private static void OnSetVisibility(ZNetView nview, long sender, int value)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        if (!nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        if (!Enum.IsDefined(typeof(PortalVisibility), value))
        {
            return;
        }

        nview.GetZDO().Set(PortalRegistry.VisibilityHash, value);
    }

    /// <summary>
    /// Runs on the ZDO's owner, for instructions from the server only. A positive stamp makes the portal home, and the
    /// home portal is always public; a negative one records that it stopped being home.
    /// </summary>
    private static void OnSetHome(ZNetView nview, long sender, long stamp)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID() || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        zdo.Set(PortalRegistry.HomeHash, stamp);
        if (stamp > 0L)
        {
            zdo.Set(PortalRegistry.VisibilityHash, (int)PortalVisibility.Public);
        }
    }
}

/// <summary>
/// Shift + Use asks the server to toggle visibility; plain Use keeps vanilla's tag-naming behaviour.
/// </summary>
[HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
internal static class TeleportWorld_Interact_Patch
{
    private static bool Prefix(TeleportWorld __instance, Humanoid human, bool hold, bool alt, ref bool __result)
    {
        if (!alt || hold)
        {
            return true;
        }

        ZNetView nview = __instance.GetComponent<ZNetView>();
        ZDO zdo = nview != null ? nview.GetZDO() : null;
        if (zdo == null || human is not Player)
        {
            return true;
        }

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

/// <summary>
/// Append the current visibility, whether this is the home portal, and the key hints to the hover text.
/// </summary>
[HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
internal static class TeleportWorld_GetHoverText_Patch
{
    private static void Postfix(TeleportWorld __instance, ref string __result)
    {
        ZNetView nview = __instance.GetComponent<ZNetView>();
        ZDO zdo = nview != null ? nview.GetZDO() : null;
        if (zdo == null)
        {
            return;
        }

        PortalInfo info = PortalRegistry.ToInfo(zdo);
        string state = info.Visibility == PortalVisibility.Public
            ? "$portalnetwork_visibility_public"
            : "$portalnetwork_visibility_private";
        string text = "\n$portalnetwork_visibility: " + state;
        bool isHome = HomePortal.IsHome(zdo.m_uid);
        if (isHome)
        {
            text += "\n<color=" + HomeColorHex + ">$portalnetwork_home_portal</color>";
        }
        else if (HomePortal.HasHome)
        {
            text += Hint(HomePortal.KeyText(PortalNetworkPlugin._travelHomeKey.Value), "$portalnetwork_travel_home_hint");
        }

        // Cosmetic only: the server makes the real decision when a key is pressed.
        Player local = Player.m_localPlayer;
        if (local != null)
        {
            Requester who = new(local.GetPlayerID(), PlayerIdentity.LocalIsAdmin());
            if (!isHome && PortalRegistry.CanChange(info, who))
            {
                text += Hint("Shift + $KEY_Use", "$portalnetwork_toggle_hint");
            }

            if (PortalRegistry.CanSetHome(who))
            {
                text += Hint(HomePortal.KeyText(PortalNetworkPlugin._setHomeKey.Value),
                    isHome ? "$portalnetwork_remove_home_hint" : "$portalnetwork_set_home_hint");
            }
        }

        __result += PortalNetworkLocalization.Localize(text);
    }

    private static string HomeColorHex => "#" + ColorUtility.ToHtmlStringRGB(PortalPinStyle.HomeColor);

    private static string Hint(string keys, string label)
    {
        return "\n[<color=yellow><b>" + keys + "</b></color>] " + label;
    }
}
