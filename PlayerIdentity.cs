namespace PortalNetwork
{
    internal struct Requester
    {
        public long PlayerId;
        public bool IsAdmin;

        public Requester(long playerId, bool isAdmin)
        {
            PlayerId = playerId;
            IsAdmin = isAdmin;
        }
    }

    internal static class PlayerIdentity
    {
        /// <summary>
        /// Server-side: who sent an RPC, taken from the server's own records instead of anything
        /// the client claims. PlayerId is 0 if the peer has no character yet.
        /// </summary>
        internal static Requester Resolve(long senderPeerId)
        {
            ZDOID characterId;
            bool isAdmin;

            if (senderPeerId == ZDOMan.GetSessionID())
            {
                // The host's own request: no ZNetPeer exists for yourself.
                characterId = ZNet.instance.LocalPlayerCharacterID;
                isAdmin = ZNet.instance.LocalPlayerIsAdminOrHost();
            }
            else
            {
                ZNetPeer peer = ZNet.instance.GetPeer(senderPeerId);
                characterId = peer != null ? peer.m_characterID : ZDOID.None;
                isAdmin = peer != null && ZNet.instance.IsAdmin(peer.m_rpc.GetSocket().GetHostName());
            }

            long playerId = 0L;
            if (!characterId.IsNone())
            {
                ZDO zdo = ZDOMan.instance.GetZDO(characterId);
                if (zdo != null) playerId = zdo.GetLong(ZDOVars.s_playerID, 0L);
            }

            return new Requester(playerId, isAdmin);
        }
    }
}
