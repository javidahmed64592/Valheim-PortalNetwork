using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using HarmonyLib;

using Jotunn;
using Jotunn.Utils;

using UnityEngine;

namespace PortalNetwork;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public class PortalNetworkPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "javidahmed64592.portalnetwork";
    public const string PluginName = "PortalNetwork";
    public const string PluginVersion = "0.3.2";

    internal static ManualLogSource _log;
    internal static ConfigEntry<KeyboardShortcut> _toggleKey;
    internal static ConfigEntry<float> _otherPinOpacity;
    private Harmony _harmony;

    private void Awake()
    {
        _log = Logger;
        _toggleKey = Config.Bind(
            "Map",
            "TogglePortalIcons",
            new KeyboardShortcut(KeyCode.P),
            "Press while the map is open to show or hide portal icons on it.");
        _otherPinOpacity = Config.Bind(
            "Map",
            "PickerOtherIconOpacity",
            0.4f,
            new ConfigDescription(
                "Opacity of non-portal map icons while choosing a destination portal (1 = unchanged, 0 = hidden).",
                new AcceptableValueRange<float>(0f, 1f)));
        PortalNetworkLocalization.Register();
        PortalNetworkRpc.Register();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
