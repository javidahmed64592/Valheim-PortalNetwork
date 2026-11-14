using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using BepInEx.Configuration;

using PortalNetwork.Core;
using PortalNetwork.Map;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace PortalNetwork.UI;

/// <summary>
/// Adds our entries to the large map's key hints, built from clones of a vanilla hint (the label) and a boxed key like
/// the build hints use: "Toggle portals" on the normal map, and the home portal keys in the portal picker. Each entry
/// follows its configured key and is only shown when pressing it would do something.
/// </summary>
internal static class PortalKeyHint
{
    private sealed class Hint(string name, System.Func<KeyboardShortcut> key, System.Func<string> label, System.Func<bool> show)
    {
        public readonly string Name = name;
        public readonly System.Func<KeyboardShortcut> Key = key;
        public readonly System.Func<string> Label = label;
        public readonly System.Func<bool> Show = show;
        public GameObject Item;

        /// <summary>The key and label the item was last built for, so it is rebuilt when either changes.</summary>
        public string BuiltFor;
    }

    private const float KeyBoxMinWidth = 30f;
    /// <summary>
    /// left, right, top, bottom
    /// </summary>
    private static readonly Regex WordBreak = new("(?<=[a-z0-9])(?=[A-Z])");
    private static readonly Vector4 KeyBoxPadding = new(8f, 8f, 4f, 4f);

    private static readonly Hint[] Hints =
    [
        // The picker has its own pins and the toggle does nothing while it is open.
        new("PortalNetwork_TogglePortalIcons",
            () => PortalNetworkPlugin._toggleKey.Value,
            () => "$portalnetwork_toggle_icons_hint",
            () => !PortalMapPicker.IsActive),
        new("PortalNetwork_TravelHome",
            () => PortalNetworkPlugin._travelHomeKey.Value,
            () => "$portalnetwork_travel_home_hint",
            () => PortalMapPicker.CanTravelHome),
        new("PortalNetwork_SetHome",
            () => PortalNetworkPlugin._setHomeKey.Value,
            () => PortalMapPicker.AtHome ? "$portalnetwork_remove_home_hint" : "$portalnetwork_set_home_hint",
            () => PortalMapPicker.CanSetHome),
    ];

    private static bool _failed;
    private static Sprite _keySprite;

    /// <summary>Called every frame (from the Minimap.Update patch).</summary>
    internal static void Tick()
    {
        if (!Minimap.IsOpen())
        {
            // The map's UI persists between openings, so keep the items; only a destroyed HUD resets them.
            if (!ReferenceEquals(Hints[0].Item, null) && Hints[0].Item == null)
            {
                Reset();
            }

            return;
        }

        Transform keyboardHints = Minimap.instance.m_largeRoot != null
            ? Minimap.instance.m_largeRoot.transform.Find("KeyHints/keyboard_hints")
            : null;
        if (keyboardHints == null || !keyboardHints.gameObject.activeInHierarchy)
        {
            return;
        }

        if (Hints[0].Item == null && (_failed || !Create(keyboardHints)))
        {
            return;
        }

        bool changed = false;
        foreach (Hint hint in Hints)
        {
            bool show = hint.Show();
            if (hint.Item.activeSelf != show)
            {
                hint.Item.SetActive(show);
                changed = true;
            }

            // The key is configurable and some labels change, so rebuild the contents if either did.
            if (show && hint.BuiltFor != BuildKey(hint))
            {
                BuildContents(hint);
                changed = true;
            }
        }

        if (changed)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)keyboardHints);
        }
    }

    private static bool Create(Transform keyboardHints)
    {
        Transform template = keyboardHints.Find("RemovePin");
        if (template == null || template.Find("Label") == null)
        {
            _failed = true;
            PortalNetworkPlugin._log.LogWarning("Vanilla map key hints layout not as expected, not adding the portal key hints.");
            return false;
        }

        foreach (Hint hint in Hints)
        {
            hint.Item = Object.Instantiate(template.gameObject, keyboardHints, false);
            hint.Item.name = hint.Name;
            hint.Item.SetActive(false);
            hint.BuiltFor = null;
        }
        return true;
    }

    private static string BuildKey(Hint hint)
    {
        return hint.Key().ToString() + "|" + hint.Label();
    }

    /// <summary>
    /// Label followed by a boxed key per part of the configured shortcut, e.g. [Left Ctrl] + [P].
    /// </summary>
    private static void BuildContents(Hint hint)
    {
        KeyboardShortcut shortcut = hint.Key();
        hint.BuiltFor = BuildKey(hint);

        Transform item = hint.Item.transform;
        Transform label = item.Find("Label");

        // Remove everything but the label (e.g. the mouse icon, or the keys from a previous build).
        for (int i = item.childCount - 1; i >= 0; i--)
        {
            Transform child = item.GetChild(i);
            if (child != label)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        label.GetComponent<TextMeshProUGUI>().text = PortalNetworkLocalization.Localize(hint.Label());

        List<string> keys = [.. shortcut.Modifiers.Select(KeyName)];
        keys.Add(KeyName(shortcut.MainKey));

        for (int i = 0; i < keys.Count; i++)
        {
            if (i > 0)
            {
                AddPlus(label, item);
            }

            AddKey(label, item, keys[i]);
        }
    }

    private static void AddPlus(Transform labelTemplate, Transform parent)
    {
        GameObject plus = Object.Instantiate(labelTemplate.gameObject, parent, false);
        plus.name = "Plus";
        plus.GetComponent<TextMeshProUGUI>().text = "+";
    }

    private static void AddKey(Transform labelTemplate, Transform parent, string key)
    {
        GameObject box = new("key_bkg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        box.transform.SetParent(parent, false);

        Sprite sprite = FindKeySprite();
        Image image = box.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.enabled = sprite != null; // without the sprite, show just the bold key name

        HorizontalLayoutGroup layout = box.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset((int)KeyBoxPadding.x, (int)KeyBoxPadding.y, (int)KeyBoxPadding.z, (int)KeyBoxPadding.w);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = box.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        box.GetComponent<LayoutElement>().minWidth = KeyBoxMinWidth;

        // The label's own ContentSizeFitter would fight the box's layout.
        GameObject text = Object.Instantiate(labelTemplate.gameObject, box.transform, false);
        text.name = "Key";
        ContentSizeFitter textFitter = text.GetComponent<ContentSizeFitter>();
        if (textFitter != null)
        {
            Object.DestroyImmediate(textFitter);
        }

        TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = key;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>
    /// The boxed-key sprite the build hints use.
    /// </summary>
    private static Sprite FindKeySprite()
    {
        if (_keySprite == null)
        {
            _keySprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "key_base");
        }

        return _keySprite;
    }

    /// <summary>
    /// "LeftControl" -> "Left Ctrl", "Alpha1" -> "1", "P" -> "P".
    /// </summary>
    private static string KeyName(KeyCode key)
    {
        string name = key.ToString();
        if (name.StartsWith("Alpha", System.StringComparison.Ordinal))
        {
            return name.Substring(5);
        }

        if (name.StartsWith("Keypad", System.StringComparison.Ordinal))
        {
            return "Num " + name.Substring(6);
        }

        name = name.Replace("Control", "Ctrl");
        return WordBreak.Replace(name, " ");
    }

    private static void Reset()
    {
        foreach (Hint hint in Hints)
        {
            hint.Item = null;
            hint.BuiltFor = null;
        }
        _failed = false;
    }
}
