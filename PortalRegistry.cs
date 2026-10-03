using System.Collections.Generic;
using UnityEngine;

namespace PortalNetwork
{
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
    }

    internal static class PortalRegistry
    {
        // Custom key stored on each portal's ZDO, so it persists with the world save.
        internal static readonly int VisibilityHash = "PortalNetwork_Visibility".GetStableHashCode();

        // Portals with no stored value (built before the mod, or never changed) are Public.
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

        /// <summary>Only meaningful on the server, where ZDOMan holds every portal in the world.</summary>
        internal static List<PortalInfo> GetVisibleTo(Requester who)
        {
            var result = new List<PortalInfo>();
            if (ZDOMan.instance == null) return result;

            foreach (ZDO zdo in ZDOMan.instance.GetPortalList())
            {
                PortalInfo info = ToInfo(zdo);
                if (IsVisibleTo(info, who))
                {
                    result.Add(info);
                }
            }
            return result;
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
                Visibility = (PortalVisibility)zdo.GetInt(VisibilityHash, (int)DefaultVisibility),
            };
        }
    }
}
