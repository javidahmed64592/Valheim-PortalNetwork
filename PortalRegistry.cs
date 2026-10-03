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

        /// <summary>
        /// Only meaningful on the server (the host's game in a listen-server setup),
        /// where ZDOMan holds every portal in the world.
        /// </summary>
        internal static List<PortalInfo> GetVisibleTo(long playerId)
        {
            var result = new List<PortalInfo>();
            if (ZDOMan.instance == null) return result;

            foreach (ZDO zdo in ZDOMan.instance.GetPortalList())
            {
                PortalInfo info = ToInfo(zdo);
                if (info.Visibility == PortalVisibility.Public || info.OwnerId == playerId)
                {
                    result.Add(info);
                }
            }
            return result;
        }

        private static PortalInfo ToInfo(ZDO zdo)
        {
            return new PortalInfo
            {
                Id = zdo.m_uid,
                Name = zdo.GetString(ZDOVars.s_tag),
                Position = zdo.GetPosition(),
                Rotation = zdo.GetRotation(),
                OwnerId = zdo.GetLong(ZDOVars.s_creator, 0L),
                Visibility = (PortalVisibility)zdo.GetInt(VisibilityHash, (int)DefaultVisibility),
            };
        }
    }
}
