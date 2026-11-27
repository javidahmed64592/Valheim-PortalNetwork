using HarmonyLib;

using PortalNetwork.Core;
using PortalNetwork.Home;

namespace PortalNetwork.Patches;

/// <summary>
/// The game updates its buttons once a frame, before anything reads them. When a portal key is used this frame, the
/// buttons that went down with it are cancelled, so the key doesn't also do what another mod bound it to.
/// </summary>
[HarmonyPatch(typeof(ZInput), nameof(ZInput.Update))]
[Feature(Feature)]
internal static class ZInput_Update_Patch
{
    /// <summary>Without it, a portal key also does whatever other mods bound to it.</summary>
    public const string Feature = "Portal keys don't trigger other bindings";

    private static void Postfix()
    {
        if (HomePortal.PortalKeyUsedThisFrame())
        {
            KeyInput.CancelButtonsPressedThisFrame();
        }
    }
}
