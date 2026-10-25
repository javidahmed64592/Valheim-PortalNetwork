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
    public const string PluginVersion = "0.3.0";

    internal static ManualLogSource _log;
    internal static ConfigEntry<KeyboardShortcut> _toggleKey;
    private Harmony _harmony;

    private void Awake()
    {
        _log = Logger;
        _toggleKey = Config.Bind(
            "Map",
            "TogglePortalIcons",
            new KeyboardShortcut(KeyCode.P),
            "Press while the map is open to show or hide portal icons on it.");
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
