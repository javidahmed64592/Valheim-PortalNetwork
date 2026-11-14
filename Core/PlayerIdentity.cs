using Jotunn.Managers;

namespace PortalNetwork.Core;

internal struct Requester(long playerId, bool isAdmin)
{
    public long PlayerId = playerId;
    public bool IsAdmin = isAdmin;
}

internal static class PlayerIdentity
{
    /// <summary>
    /// Client-side, for showing or hiding hints: whether the local player is an admin or the host. The server checks
    /// again whenever it matters. Vanilla's LocalPlayerIsAdminOrHost compares against a copy of the admin list that
    /// doesn't match players on a crossplay dedicated server, so this uses the status Jotunn gets from the server,
    /// which the server decides the same way as <see cref="Resolve"/>.
    /// </summary>
    internal static bool LocalIsAdmin()
    {
        return ZNet.instance != null
            && (ZNet.instance.IsServer() || SynchronizationManager.Instance.PlayerIsAdmin);
    }

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
            if (zdo != null)
            {
                playerId = zdo.GetLong(ZDOVars.s_playerID, 0L);
            }
        }

        return new Requester(playerId, isAdmin);
    }
}
