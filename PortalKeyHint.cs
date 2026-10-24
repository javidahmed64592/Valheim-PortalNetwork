using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using BepInEx.Configuration;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace PortalNetwork;

/// <summary>
/// Adds a "Toggle portals" entry for the portal icon key to the large map's key hints, built from clones
/// of a vanilla hint (the label) and a boxed key like the build hints use. It follows the configured key.
/// </summary>
internal static class PortalKeyHint
{
    private const string ItemName = "PortalNetwork_TogglePortalIcons";
    private const float KeyBoxMinWidth = 30f;
    /// <summary>
    /// left, right, top, bottom
    /// </summary>
    private static readonly Regex WordBreak = new("(?<=[a-z0-9])(?=[A-Z])");
    private static readonly Vector4 KeyBoxPadding = new(8f, 8f, 4f, 4f);

    private static GameObject _item;
    private static string _builtFor;
    private static bool _failed;
    private static Sprite _keySprite;

    /// <summary>Called every frame (from the Minimap.Update patch).</summary>
    internal static void Tick()
    {
        if (!Minimap.IsOpen())
        {
            // The map's UI persists between openings, so keep the item; only a destroyed HUD resets it.
            if (!ReferenceEquals(_item, null) && _item == null)
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

        if (_item == null && (_failed || !Create(keyboardHints)))
        {
            return;
        }

        // The picker has its own pins and the toggle does nothing while it is open.
        bool show = !PortalMapPicker.IsActive;
        if (_item.activeSelf != show)
        {
            _item.SetActive(show);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)keyboardHints);
        }

        // The key is configurable, so rebuild the key boxes if it changed.
        if (_builtFor != PortalNetworkPlugin._toggleKey.Value.ToString())
        {
            BuildContents();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)keyboardHints);
        }
    }

    private static bool Create(Transform keyboardHints)
    {
        Transform template = keyboardHints.Find("RemovePin");
        if (template == null || template.Find("Label") == null)
        {
            _failed = true;
            PortalNetworkPlugin._log.LogWarning("Vanilla map key hints layout not as expected, not adding the portal icon key hint.");
            return false;
        }

        _item = Object.Instantiate(template.gameObject, keyboardHints, false);
        _item.name = ItemName;
        _item.SetActive(true);

        BuildContents();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)keyboardHints);
        return true;
    }

    /// <summary>
    /// Label followed by a boxed key per part of the configured shortcut, e.g. [Left Ctrl] + [P].
    /// </summary>
    private static void BuildContents()
    {
        KeyboardShortcut shortcut = PortalNetworkPlugin._toggleKey.Value;
        _builtFor = shortcut.ToString();

        Transform item = _item.transform;
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

        label.GetComponent<TextMeshProUGUI>().text = PortalNetworkLocalization.Localize("$portalnetwork_toggle_icons_hint");

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
        _item = null;
        _builtFor = null;
        _failed = false;
    }
}