using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Jotunn.Utils;

namespace PortalNetwork
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class PortalNetworkPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "javidahmed64592.portalnetwork";
        public const string PluginName = "PortalNetwork";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
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
}
