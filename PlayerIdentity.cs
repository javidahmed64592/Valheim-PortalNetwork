namespace PortalNetwork
{
    internal static class PlayerIdentity
    {
        /// <summary>
        /// Server-side: the player ID (the value Player.GetPlayerID() returns) for the peer that sent
        /// an RPC, taken from the server's own records instead of anything the client claims.
        /// Returns 0 if the peer has no character yet.
        /// </summary>
        internal static long ResolvePlayerId(long senderPeerId)
        {
            ZDOID characterId;
            if (senderPeerId == ZDOMan.GetSessionID())
            {
                // The host's own request: no ZNetPeer exists for yourself.
                characterId = ZNet.instance.LocalPlayerCharacterID;
            }
            else
            {
                ZNetPeer peer = ZNet.instance.GetPeer(senderPeerId);
                characterId = peer != null ? peer.m_characterID : ZDOID.None;
            }

            if (characterId.IsNone()) return 0L;

            ZDO zdo = ZDOMan.instance.GetZDO(characterId);
            return zdo != null ? zdo.GetLong(ZDOVars.s_playerID, 0L) : 0L;
        }
    }
}
