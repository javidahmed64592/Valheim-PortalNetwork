using System.Collections.Generic;

using HarmonyLib;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PortalNetwork.Core;

/// <summary>
/// Key handling shared by the mod's keys: when a shortcut counts as pressed, when the player is typing, and keeping a
/// key the mod has used from also doing what other mods bound it to.
/// </summary>
internal static class KeyInput
{
    private static readonly AccessTools.FieldRef<ZInput, Dictionary<string, ZInput.ButtonDef>> Buttons =
        AccessTools.FieldRefAccess<ZInput, Dictionary<string, ZInput.ButtonDef>>("m_buttons");

    /// <summary>
    /// True while the player is typing: in chat, the console, a game text box (such as naming a portal), a map pin's
    /// name, or any focused text field. Letters typed there must not trigger the mod's keys.
    /// </summary>
    internal static bool IsTyping()
    {
        if (Minimap.InTextInput()
            || (Chat.instance != null && Chat.instance.HasFocus())
            || Console.IsVisible()
            || TextInput.IsVisible())
        {
            return true;
        }

        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && ((selected.TryGetComponent(out InputField field) && field.isFocused)
            || (selected.TryGetComponent(out TMP_InputField tmpField) && tmpField.isFocused));
    }

    /// <summary>
    /// Cancels, for the rest of this frame, every game button that went down this frame. The game and mods built on
    /// Jotunn read their keys through these buttons, so a key this mod has just used (say B to go back while looking
    /// at a portal) doesn't also do what another mod bound it to (BetterHammer's build menu). Run straight after the
    /// game updates its buttons each frame, before anything reads them. Mods that read the keyboard directly can't be
    /// stopped this way.
    /// </summary>
    internal static void CancelButtonsPressedThisFrame()
    {
        ZInput input = ZInput.instance;
        if (input == null)
        {
            return;
        }

        foreach (ZInput.ButtonDef button in Buttons(input).Values)
        {
            if (button.Pressed)
            {
                button.ResetState();
            }
        }
    }
}
