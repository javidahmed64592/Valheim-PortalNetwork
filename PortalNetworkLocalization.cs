using System.Collections.Generic;
using Jotunn.Managers;

namespace PortalNetwork
{
    internal static class PortalNetworkLocalization
    {
        internal static void Register()
        {
            LocalizationManager.Instance.GetLocalization().AddTranslation("English", new Dictionary<string, string>
            {
                { "portalnetwork_default_name", "Portal" },
                { "portalnetwork_visibility", "Visibility" },
                { "portalnetwork_visibility_public", "Public" },
                { "portalnetwork_visibility_private", "Private" },
                { "portalnetwork_private_suffix", "private" },
                { "portalnetwork_toggle_hint", "Toggle visibility" },
                { "portalnetwork_no_other_portals", "No other portals available" },
                { "portalnetwork_portal_unavailable", "That portal is no longer available" },
                { "portalnetwork_portal_missing", "That portal no longer exists" },
                { "portalnetwork_only_builder", "Only the builder can change who sees this portal" },
                { "portalnetwork_move_closer", "Move closer to the portal and try again" },
                { "portalnetwork_now_public", "Portal visible to everyone" },
                { "portalnetwork_now_private", "Portal visible only to its builder" },
                { "portalnetwork_version_mismatch", "PortalNetwork version mismatch between server and client" },
            });
        }

        /// <summary>Translate a string containing $tokens into the player's current language.</summary>
        internal static string Localize(string text)
        {
            return Localization.instance.Localize(text);
        }
    }
}
