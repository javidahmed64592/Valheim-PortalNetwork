using System.Collections.Generic;
using System.Globalization;

using UnityEngine;

namespace PortalNetwork;

/// <summary>
/// Per-character, per-world favourites and last trip, kept in the character's save (Player.m_customData).
/// Portals are identified by their rounded position: vanilla reassigns ZDO ids every time a world loads,
/// but placed portals never move.
/// </summary>
internal static class PortalFavourites
{
    private const string FavouritesPrefix = "PortalNetwork_Favourites_";
    private const string LastTripPrefix = "PortalNetwork_LastTrip_";
    private const string FavouritesOnlyKey = "PortalNetwork_FavouritesOnly";
    private const char ListSeparator = ';';
    private const char TripSeparator = '|';

    internal static string Key(Vector3 position)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0},{1},{2}",
            Mathf.RoundToInt(position.x),
            Mathf.RoundToInt(position.y),
            Mathf.RoundToInt(position.z));
    }

    internal static HashSet<string> GetFavourites()
    {
        HashSet<string> result = [];
        if (TryGet(FavouritesPrefix, out string value))
        {
            foreach (string key in value.Split(ListSeparator))
            {
                if (key.Length > 0)
                {
                    _ = result.Add(key);
                }
            }
        }
        return result;
    }

    internal static void SetFavourite(PortalInfo portal, bool favourite)
    {
        HashSet<string> favourites = GetFavourites();
        string key = Key(portal.Position);
        bool changed = favourite ? favourites.Add(key) : favourites.Remove(key);
        if (changed)
        {
            Set(FavouritesPrefix, string.Join(ListSeparator.ToString(), favourites));
        }
    }

    internal static bool FavouritesOnly
    {
        get
        {
            Player player = Player.m_localPlayer;
            return player != null
                && player.m_customData.TryGetValue(FavouritesOnlyKey, out string value)
                && value == "1";
        }
        set
        {
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                player.m_customData[FavouritesOnlyKey] = value ? "1" : "0";
            }
        }
    }

    internal static void RecordTrip(Vector3 from, Vector3 to)
    {
        Set(LastTripPrefix, Key(from) + TripSeparator + Key(to));
    }

    /// <summary>
    /// Where "go back" should lead from the portal at <paramref name="current"/>: the start of the last trip
    /// if we are standing at its end (so two portals can be ping-ponged), otherwise the last destination.
    /// </summary>
    internal static bool TryGetGoBackKey(Vector3 current, out string key)
    {
        key = null;
        if (!TryGet(LastTripPrefix, out string value))
        {
            return false;
        }

        string[] parts = value.Split(TripSeparator);
        if (parts.Length != 2)
        {
            return false;
        }

        string here = Key(current);
        key = here == parts[1] ? parts[0] : parts[1];
        return key != here;
    }

    private static bool TryGet(string prefix, out string value)
    {
        value = null;
        Player player = Player.m_localPlayer;
        return player != null
            && TryGetWorldKey(prefix, out string fullKey)
            && player.m_customData.TryGetValue(fullKey, out value);
    }

    private static void Set(string prefix, string value)
    {
        Player player = Player.m_localPlayer;
        if (player != null && TryGetWorldKey(prefix, out string fullKey))
        {
            player.m_customData[fullKey] = value;
        }
    }

    private static bool TryGetWorldKey(string prefix, out string fullKey)
    {
        World world = ZNet.World;
        fullKey = world != null ? prefix + world.m_uid.ToString(CultureInfo.InvariantCulture) : null;
        return world != null;
    }
}
