using BepInEx;
using BepInEx.Configuration;

using UnityEngine;

namespace PortalNetwork.Core;

/// <summary>
/// BepInEx's <see cref="KeyboardShortcut.IsDown"/> is false while any key outside the shortcut is held, so holding a
/// movement key or Shift to run blocked every key of this mod. Here a shortcut is pressed when its main key goes down
/// while its modifiers are held, whatever else is held.
/// </summary>
internal static class KeyInput
{
    internal static bool IsDown(KeyboardShortcut shortcut)
    {
        if (shortcut.MainKey == KeyCode.None || !UnityInput.Current.GetKeyDown(shortcut.MainKey))
        {
            return false;
        }

        foreach (KeyCode modifier in shortcut.Modifiers)
        {
            if (!UnityInput.Current.GetKey(modifier))
            {
                return false;
            }
        }
        return true;
    }
}
