using System.Collections;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace PortalNetwork
{
    internal static class PortalNetworkRpc
    {
        private const string RequestName = "PortalNetwork_ListRequest";
        private const string ResponseName = "PortalNetwork_ListResponse";

        private static CustomRPC _request;
        private static CustomRPC _response;

        internal static void Register()
        {
            _request = NetworkManager.Instance.AddRPC(RequestName, OnRequestReceived, IgnoreMessage);
            _response = NetworkManager.Instance.AddRPC(ResponseName, OnResponseReceived, OnResponseReceived);
        }

        /// <summary>Client to server: ask for the portals this player may see.</summary>
        internal static void SendListRequest(long playerId)
        {
            var package = new ZPackage();
            package.Write(playerId);
            _request.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
        }

        // Runs on the server (the host's game in a listen-server setup).
        private static IEnumerator OnRequestReceived(long sender, ZPackage package)
        {
            long playerId = package.ReadLong();
            List<PortalInfo> visible = PortalRegistry.GetVisibleTo(playerId);
            PortalNetworkPlugin.Log.LogInfo(
                $"Server: list request from peer {sender} (player {playerId}), sending {visible.Count} portal(s)");

            var response = new ZPackage();
            response.Write(visible.Count);
            foreach (PortalInfo p in visible)
            {
                response.Write(p.Id);
                response.Write(p.Name ?? "");
                response.Write(p.Position);
                response.Write(p.Rotation);
                response.Write(p.OwnerId);
                response.Write((int)p.Visibility);
            }

            _response.SendPackage(sender, response);
            yield break;
        }

        // Runs on whoever receives the response (a client, or the host itself).
        private static IEnumerator OnResponseReceived(long sender, ZPackage package)
        {
            int count = package.ReadInt();
            var portals = new List<PortalInfo>(count);
            for (int i = 0; i < count; i++)
            {
                portals.Add(new PortalInfo
                {
                    Id = package.ReadZDOID(),
                    Name = package.ReadString(),
                    Position = package.ReadVector3(),
                    Rotation = package.ReadQuaternion(),
                    OwnerId = package.ReadLong(),
                    Visibility = (PortalVisibility)package.ReadInt(),
                });
            }

            PortalNetworkClient.OnPortalListReceived(portals);
            yield break;
        }

        private static IEnumerator IgnoreMessage(long sender, ZPackage package)
        {
            yield break;
        }
    }
}
