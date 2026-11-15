using HarmonyLib;

using PortalNetwork.Core;
using PortalNetwork.Home;

namespace PortalNetwork.Patches;

/// <summary>
/// The game updates its buttons once a frame, before anything reads them. When a portal key is used this frame, the
/// buttons that went down with it are cancelled, so the key doesn't also do what another mod bound it to.
/// </summary>
[HarmonyPatch(typeof(ZInput), nameof(ZInput.Update))]
internal static class ZInput_Update_Patch
{
    private static void Postfix()
    {
        if (HomePortal.PortalKeyUsedThisFrame())
        {
            KeyInput.CancelButtonsPressedThisFrame();
        }
    }
}
