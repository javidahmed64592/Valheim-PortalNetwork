using System.Collections.Generic;

using PortalNetwork.Core;

using UnityEngine;

namespace PortalNetwork.Portals;

internal enum PortalVisibility
{
    Private = 0,
    Public = 1,
}

internal struct PortalInfo
{
    public ZDOID Id;
    public string Name;
    public string TagAuthor;
    public Vector3 Position;
    public Quaternion Rotation;
    public long OwnerId;
    public PortalVisibility Visibility;
    public bool IsHome;
}

internal static class PortalRegistry
{
    /// <summary>
    /// Custom key stored on each portal's ZDO, so it persists with the world save.
    /// </summary>
    internal static readonly int VisibilityHash = "PortalNetwork_Visibility".GetStableHashCode();

    /// <summary>
    /// Server time (ticks) when an admin made this portal the world's home portal, or minus that time when they
    /// removed it. The latest change across all portals decides: if it set a portal that is still public, that portal
    /// is home, otherwise there is none. Setting a new home never has to edit the old one, which may be far away with
    /// nobody to own it.
    /// </summary>
    internal static readonly int HomeHash = "PortalNetwork_Home".GetStableHashCode();

    /// <summary>
    /// Portals with no stored value (built before the mod, or never changed) are Public.
    /// </summary>
    private const PortalVisibility DefaultVisibility = PortalVisibility.Public;

    /// <summary>True if the ZDO exists right now and is a portal. Server-side only.</summary>
    internal static bool TryGetPortal(ZDOID id, out ZDO zdo)
    {
        zdo = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(id) : null;
        return zdo != null && Game.instance.PortalPrefabHash.Contains(zdo.GetPrefab());
    }

    internal static bool IsVisibleTo(PortalInfo info, Requester who)
    {
        return who.IsAdmin
            || info.Visibility == PortalVisibility.Public
            || (who.PlayerId != 0L && info.OwnerId == who.PlayerId);
    }

    /// <summary>The builder or an admin may change it. Portals with no recorded builder are open to anyone.</summary>
    internal static bool CanChange(PortalInfo info, Requester who)
    {
        return who.IsAdmin
            || info.OwnerId == 0L
            || (who.PlayerId != 0L && info.OwnerId == who.PlayerId);
    }

    /// <summary>Only admins and the host choose the home portal, since it is shared by everyone in the world.</summary>
    internal static bool CanSetHome(Requester who)
    {
        return who.IsAdmin;
    }

    /// <summary>The world's home portal, or ZDOID.None if none is set. Server-side only.</summary>
    internal static ZDOID FindHome()
    {
        ZDOID home = ZDOID.None;
        long latest = 0L;
        if (ZDOMan.instance == null)
        {
            return home;
        }

        foreach (ZDO zdo in ZDOMan.instance.GetPortalList())
        {
            long stamp = zdo.GetLong(HomeHash, 0L);
            long time = System.Math.Abs(stamp);
            if (time > latest)
            {
                latest = time;
                home = stamp > 0L && GetVisibility(zdo) == PortalVisibility.Public ? zdo.m_uid : ZDOID.None;
            }
        }
        return home;
    }

    /// <summary>Only meaningful on the server, where ZDOMan holds every portal in the world.</summary>
    internal static List<PortalInfo> GetVisibleTo(Requester who)
    {
        List<PortalInfo> result = [];
        if (ZDOMan.instance == null)
        {
            return result;
        }

        ZDOID home = FindHome();
        foreach (ZDO zdo in ZDOMan.instance.GetPortalList())
        {
            PortalInfo info = ToInfo(zdo);
            info.IsHome = zdo.m_uid == home;
            if (IsVisibleTo(info, who))
            {
                result.Add(info);
            }
        }
        return result;
    }

    private static PortalVisibility GetVisibility(ZDO zdo)
    {
        return (PortalVisibility)zdo.GetInt(VisibilityHash, (int)DefaultVisibility);
    }

    internal static PortalInfo ToInfo(ZDO zdo)
    {
        return new PortalInfo
        {
            Id = zdo.m_uid,
            Name = zdo.GetString(ZDOVars.s_tag),
            TagAuthor = zdo.GetString(ZDOVars.s_tagauthor),
            Position = zdo.GetPosition(),
            Rotation = zdo.GetRotation(),
            OwnerId = zdo.GetLong(ZDOVars.s_creator, 0L),
            Visibility = GetVisibility(zdo),
        };
    }
}
