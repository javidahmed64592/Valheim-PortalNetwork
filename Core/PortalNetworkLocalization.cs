using System.Collections.Generic;

using Jotunn.Managers;

namespace PortalNetwork.Core;

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
            { "portalnetwork_toggle_icons_hint", "Toggle portals" },
            { "portalnetwork_no_other_portals", "No other portals available" },
            { "portalnetwork_portal_unavailable", "That portal is no longer available" },
            { "portalnetwork_portal_missing", "That portal no longer exists" },
            { "portalnetwork_only_builder", "Only the builder can change who sees this portal" },
            { "portalnetwork_move_closer", "Move closer to the portal and try again" },
            { "portalnetwork_now_public", "Portal visible to everyone" },
            { "portalnetwork_now_private", "Portal visible only to its builder" },
            { "portalnetwork_panel_title", "Portals" },
            { "portalnetwork_go_back", "Go back to" },
            { "portalnetwork_no_last_portal", "No previous portal" },
            { "portalnetwork_here", "You are here" },
            { "portalnetwork_home_portal", "Home portal" },
            { "portalnetwork_travel_home_hint", "Travel home" },
            { "portalnetwork_set_home_hint", "Set as home portal" },
            { "portalnetwork_remove_home_hint", "Remove home portal" },
            { "portalnetwork_go_back_hint", "Go back" },
            { "portalnetwork_group_biome", "Biome" },
            { "portalnetwork_group_distance", "Distance" },
            { "portalnetwork_group_here", "Around you" },
            { "portalnetwork_group_other", "Elsewhere" },
            { "portalnetwork_dir_n", "north" },
            { "portalnetwork_dir_ne", "north-east" },
            { "portalnetwork_dir_e", "east" },
            { "portalnetwork_dir_se", "south-east" },
            { "portalnetwork_dir_s", "south" },
            { "portalnetwork_dir_sw", "south-west" },
            { "portalnetwork_dir_w", "west" },
            { "portalnetwork_dir_nw", "north-west" },
            { "portalnetwork_search_placeholder", "Search by name or biome" },
            { "portalnetwork_no_matches", "No portals match your search" },
            { "portalnetwork_renamed", "Portal renamed" },
            { "portalnetwork_name_locked", "Only the builder can rename this portal" },
            { "portalnetwork_name_locked_status", "Name locked" },
            { "portalnetwork_only_builder_lock", "Only the builder can lock this portal's name" },
            { "portalnetwork_now_name_locked", "Name locked: only the builder and admins can rename this portal" },
            { "portalnetwork_now_name_unlocked", "Name unlocked: anyone can rename this portal" },
            { "portalnetwork_home_set", "This is now the home portal" },
            { "portalnetwork_home_removed", "This is no longer the home portal" },
            { "portalnetwork_not_home", "This is not the home portal" },
            { "portalnetwork_already_home", "This is already the home portal" },
            { "portalnetwork_only_admin_home", "Only admins can choose the home portal" },
            { "portalnetwork_no_home", "No home portal has been set" },
            { "portalnetwork_home_must_be_public", "The home portal must stay public" },
            { "portalnetwork_version_mismatch", "PortalNetwork version mismatch between server and client" },
        });
    }

    /// <summary>Translate a string containing $tokens into the player's current language.</summary>
    internal static string Localize(string text)
    {
        return Localization.instance.Localize(text);
    }
}
